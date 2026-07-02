# Auditoria — Endpoint de Envio de Emails com Multi-Provider Fallback

**Data:** 2026-07-01
**Escopo:** `api-core` (branch `main`, HEAD local `db16787`, origin/main 1 commit atrás — apenas `security: untrack .playwright-mcp`)
**Método:** leitura direta do código + 3 varreduras independentes (fluxo/endpoints, testes, banco) + verificação HTTP ao vivo dos endpoints de produção. Cada achado está marcado como **[FATO]** (verificado no código/rede) ou **[HIPÓTESE]** (inferência condicional).

---

## 1. Resumo Executivo

O DIAX CRM tem **dois sistemas de envio de email paralelos e desconectados**:

- **Sistema A — Dispatch unificado** (`POST /api/v1/integrations/send-email` → `EmailFallbackOrchestrator`): fallback real entre providers, idempotência por chave, quota diária/semanal, circuit breaker por provider, log PII-safe por tentativa. É o "endpoint com vários fallbacks". Arquitetura boa no desenho, mas com defeitos sérios de resiliência: **breaker que nunca fecha**, **idempotência com corrida**, **timeout que aborta a cadeia inteira**, e **zero testes na implementação real**.
- **Sistema B — Fila de campanhas** (`/api/v1/email-campaigns/*` + `/api/v1/email-providers/queue-with-assignment` → `EmailQueueProcessorWorker`): enfileira com provider fixo por item, **sem fallback entre providers**, retry com backoff no mesmo provider, circuit breaker global "piloto" com reset manual.

**Veredito antecipado: NÃO está pronto para produção sem correções** (justificativa objetiva na seção 12). O sistema funciona no caminho feliz — milhares de emails enviados — mas há **3 riscos P0 confirmados**: (1) **todo email de campanha sai com link de unsubscribe morto** (domínio, rota e parâmetro errados); (2) **o caminho de enfileiramento usado pelas campanhas reais não checa opt-out nem lista de supressão**; (3) **crash entre envio e gravação deixa itens presos em `Processing` para sempre** — perda silenciosa, sem recuperação.

---

## 2. Arquitetura Atual

```
SISTEMA A (transacional / server-to-server)
  POST /api/v1/integrations/send-email  [AllowAnonymous + X-Integration-Key]
    └─ EmailFallbackOrchestrator (Diax.Application/EmailMarketing/Dispatch)
         ├─ Idempotência (email_send_log, janela 24h)
         ├─ Validação de domínio remetente (EmailChain:SenderDomains)
         ├─ Cadeia Tier1 (ESPs DKIM-alinhados) → Tier2 (SMTP próprio, só se allowUnaligned=true)
         ├─ EmailProviderCircuitBreaker (por provider, in-memory, Singleton)
         ├─ ProviderQuotaGuard (diário + semanal, in-memory, Singleton)
         └─ IEmailSender keyed por provider (6 ESPs + 4-7 SMTPs)

SISTEMA B (campanhas de marketing)
  POST /api/v1/email-campaigns/{id}/queue           [JWT + campaigns.manage] — round-robin 6 providers
  POST /api/v1/email-providers/queue-with-assignment [JWT + campaigns.manage] — provider escolhido pelo cliente
  POST /api/v1/email-campaigns/send-single | send-bulk — default Brevo
    └─ grava email_queue_items (Status=Queued, AssignedProvider fixo)
  EmailQueueProcessorWorker (BackgroundService, ciclo de 5 min)
    ├─ PilotCircuitBreaker (GLOBAL, in-memory, reset manual via POST /pilot/reset)
    ├─ Limites globais Daily/Hourly (contados do banco) + batch 20/provider/ciclo
    ├─ Para cada provider: pega lote Queued → MarkProcessing → SendAsync → MarkSent/MarkFailed
    └─ Retry: backoff exponencial 2^n × 15min, máx 3 tentativas, MESMO provider
```

**Arquivos-chave:**

| Papel | Arquivo |
|---|---|
| Endpoint A | `api-core/src/Diax.Api/Controllers/V1/IntegrationsController.cs:139-231` |
| Orquestrador fallback | `api-core/src/Diax.Application/EmailMarketing/Dispatch/EmailFallbackOrchestrator.cs` |
| Breaker por provider (A) | `api-core/src/Diax.Infrastructure/Email/EmailProviderCircuitBreaker.cs` |
| Quota guard (A) | `api-core/src/Diax.Infrastructure/Email/ProviderQuotaGuard.cs` |
| Classificador de erros | `api-core/src/Diax.Application/EmailMarketing/EmailErrorClassifier.cs` |
| Endpoints B | `Controllers/V1/EmailCampaignsController.cs`, `Controllers/V1/EmailProvidersController.cs:60` |
| Serviço de campanhas | `api-core/src/Diax.Application/EmailMarketing/EmailMarketingService.cs` (1.365 linhas) |
| Worker da fila | `api-core/src/Diax.Infrastructure/Email/EmailQueueProcessorWorker.cs` |
| Breaker global (B) | `api-core/src/Diax.Application/EmailMarketing/PilotCircuitBreaker.cs` |
| Senders (6 ESPs) | `api-core/src/Diax.Infrastructure/Email/{Brevo,Mailjet,Resend,ElasticEmail,MailerSend,SendGrid}EmailSender.cs` + `GenericSmtpEmailSender.cs` |
| Config | `api-core/src/Diax.Api/appsettings.json:226-270` (`EmailChain`) |
| Deploy/secrets | `.github/workflows/deploy-api-core-smarterasp.yml` (jq gera appsettings.Production.json) |
| Unsubscribe real | `Controllers/V1/EmailUnsubscribeController.cs` (`GET /unsubscribe?token=<HMAC>`) |

---

## 3. Fluxo Atual em Passos

### Sistema A — `POST /api/v1/integrations/send-email`

1. **Auth** — header `X-Integration-Key` comparado em tempo constante com `Integrations:SendEmailKey`; 503 se não configurada (fail-closed), 401 se inválida (`IntegrationsController.cs:145-157`). [FATO]
2. **Validação** — `fromEmail`, `to[]` (≥1) e `html` obrigatórios; render de `{{Variables}}` via `TemplateRenderer.RenderAll` (`:162-175`).
3. **Idempotência** — se `IdempotencyKey` presente: busca em `email_send_log` (24h); `Sent`→200 `Duplicate`; `InFlight`→409 `InProgress` (`EmailFallbackOrchestrator.cs:44-63`).
4. **Gate de domínio** — domínio do `From` precisa existir em `EmailChain:SenderDomains`, senão 400 `Rejected` (`:66-71`).
5. **Log `InFlight`** persistido ANTES de enviar (`:74-76`) — bom para durabilidade.
6. **Cadeia de providers** — Tier1 na ordem configurada; Tier2 só se `allowUnaligned=true` (`:79-97`). Por provider: pula se breaker aberto (`:99`), pula se quota esgotada (`:105`), tenta `SendAsync` **uma única vez** (sem retry por provider).
7. **Timeout** — `HardTimeout` 60s para a **cadeia inteira**; estourou → grava tentativa, `RecordFailure(timeout)` e **`break` (aborta tudo)** (`:126-134`).
8. **Sucesso** → `RecordSuccess` + `MarkSent` + 200 com `{messageId, provider, attempts}`. **Falha** → `RecordFailure` e próximo provider. **Esgotou** → `MarkFailed` + 502 `AllFailed` (`:143-161`).

### Sistema B — campanha via `queue-with-assignment` (caminho usado pelos scripts `fire_w*`)

1. JWT + permissão `campaigns.manage`; campanha precisa pertencer ao usuário.
2. **PilotCircuitBreaker aberto → bloqueia tudo** com audit `PilotRealSendBlocked` (`EmailMarketingService.cs:1056-1061`).
3. **Readiness gates**: corpo com `{{unsubscribe_url}}` obrigatório, links com `utm_source=` obrigatório, campanha não pode estar `Draft` (`:1181-1228`).
4. Por lead: valida email, **deduplica contra itens já enfileirados da mesma campanha** (in-memory, sem constraint), mapeia string `assignedProvider` → enum (**desconhecido cai silenciosamente em Brevo**, `:1122-1130`).
5. Grava `email_queue_items` + `campaign.StartProcessing()` + `customer.RegisterEmailSent()` (fix PR #31, `:1164`) — **um único `SaveChanges`, atômico entre si** (`:1168`). [FATO]
6. **Worker (a cada 5 min)**: checa breaker → limites globais → por provider pega até 20 itens `Queued` → `MarkProcessing`+Save → renderiza template com variáveis do customer → `SendAsync` → `MarkSent`+`IncrementSent` ou `MarkFailed`+`IncrementFailed`+Save (`EmailQueueProcessorWorker.cs:144-241`).
7. **Retry**: itens `Failed` com `AttemptCount < 3` e <7 dias → `Requeue(backoff 2^n×15min)` **no mesmo provider** (`:255-277`; `EmailQueueItem.Requeue` não troca `AssignedProvider`). [FATO]

---

## 4. Providers e Fallback

### Ordem de fallback (Sistema A) — `appsettings.json:260-269` [FATO]

| Domínio remetente | Tier 1 (ordem) | Tier 2 (só `allowUnaligned=true`) |
|---|---|---|
| `alexandrequeiroz.com.br` | brevo → mailjet → resend → elasticemail → mailersend → sendgrid | gmail-smtp → hostgator-smtp → hostgator-contato → hostgator-envio → hostgator-marketing → hostgator-vendas → smarterasp-smtp |
| `extratordedados.com.br` | resend → elasticemail → mailersend → sendgrid → mailjet | gmail-smtp |

### Estado operacional dos providers (memória de sessões, 2026-05/06) [FATO operacional]

| Provider | Credencial | Limite diário config (A) | Observação |
|---|---|---|---|
| Brevo | ✅ OK | 280 | Default DI não-keyed; webhook de tracking implementado |
| Mailjet | ✅ OK | 180 | |
| Resend | ✅ OK | 90 | |
| SendGrid | ✅ OK | 90 | Domain auth verificada; assinatura de webhook ECDSA testada |
| MailerSend | ❌ token inválido | 90 | **100% falha** — todo item atribuído a ele morre |
| ElasticEmail | ❌ key expirada + free tier restrito | 90 | **100% falha** |

**Consequência combinada [FATO]:** o endpoint legado `POST /campaigns/{id}/queue` faz round-robin `(providerIndex % 6)` **incluindo os 2 providers mortos** (`EmailMarketingService.cs:468`) → ~1/3 dos destinatários é atribuído a provider que falhará 3× e será abandonado (retry não troca provider). Os scripts de campanha contornam isso usando `queue-with-assignment` com só os 4 saudáveis — ou seja, **a proteção está no script externo, não no sistema**.

---

## 5. Pontos Fortes

1. **Desenho do Sistema A é sério**: log `InFlight` persistido antes do envio, registro por tentativa com latência, hashes SHA-256 de destinatário/assunto/corpo (PII-safe), idempotência aplicada, separação Tier1/Tier2 por alinhamento DKIM com gate explícito (`allowUnaligned`), quota diária+semanal por provider, config hot-reload via `IOptionsMonitor`. [FATO]
2. **Auth do endpoint A fail-closed** com comparação em tempo constante (`TimingSafeEquals`). [FATO]
3. **Gates de campanha bem pensados**: unsubscribe obrigatório no corpo, UTMs obrigatórios, Draft bloqueado, circuit breaker antes de enfileirar, trilha de auditoria rica (`PilotRealSendBlocked`, `PilotReadinessBlocked`, `PilotCircuitBreakerOpened`, etc.). [FATO]
4. **Fix do PR #31 está em `main` e em `origin/main`**, e o `RegisterEmailSent()` é atômico com o insert da fila (mesmo `SaveChanges`) (`EmailMarketingService.cs:513, 1164`). [FATO]
5. **Classificação de erros compartilhada** (`EmailErrorClassifier`): 429 tratado como transitório (não abre breaker nem polui a janela) — decisão correta. [FATO]
6. **Webhooks Brevo**: delivered/opened idempotentes; assinatura SendGrid ECDSA verificada e testada. [FATO]
7. **Testes existem onde existem** (71 atributos): `PilotCircuitBreaker` (10), `ProviderQuotaGuard` (15), webhooks (10), template engine (10) são bem cobertos, determinísticos, sem rede, sem sleeps. [FATO]

---

## 6. Riscos Críticos

### P0-1 — Link de unsubscribe MORTO em todo email de campanha [FATO, verificado ao vivo]
`EmailMarketingService.cs:890` (e `:160` no send-test) gera:
```
https://diaxcrm.com.br/api/v1/email-campaigns/unsubscribe?email=<destinatário>
```
Três erros simultâneos:
- **Domínio**: `diaxcrm.com.br` não responde (verificado por HTTP em 2026-07-01 — falha de conexão; a API de produção é `api.alexandrequeiroz.com.br`).
- **Rota**: o endpoint real é `GET /unsubscribe` na raiz (`EmailUnsubscribeController.cs:19`) — não existe `api/v1/email-campaigns/unsubscribe` (confirmado 404 na API de produção).
- **Parâmetro**: o endpoint real exige `?token=<HMAC-SHA256>`, não `?email=`.

A ironia: `ValidateContentReadiness` **obriga** `{{unsubscribe_url}}` no corpo (`:1189-1192`) — ou seja, o gate garante que todo email contenha um link quebrado. **Impacto**: LGPD/CAN-SPAM (destinatário não consegue se descadastrar), reputação de envio (quem não consegue sair marca spam), e todos os +2.000 emails já enviados têm esse link.

### P0-2 — `QueueWithSmartAssignmentAsync` NÃO checa opt-out nem supressão [FATO]
O caminho legado `QueueCampaignRecipientsAsync` checa `customer.EmailOptOut` e `IsSuppressedAsync` (`EmailMarketingService.cs:450-460`). O caminho **realmente usado pelas campanhas** (`queue-with-assignment`, `:1102-1120`) checa apenas email válido + dedup da campanha. **Quem fez opt-out (inclusive via webhook de hard bounce, que seta opt-out) pode ser enfileirado de novo em qualquer campanha nova.** Violação direta de consentimento + risco de bounce recorrente.

### P0-3 — Itens presos em `Processing` = perda silenciosa [FATO]
Worker: `MarkProcessing`+Save (`:176-178`) → HTTP ao provider (`:200`) → `MarkSent/MarkFailed`+Save (`:240`). Se o processo morre no meio (deploy, recycle do app pool do SmarterASP, crash):
- `GetPendingBatchByProviderAsync` só busca `Status == Queued` (`EmailQueueRepository.cs:43`);
- `GetFailedForRetryAsync` só busca `Status == Failed` (`:111`);
- **nenhuma query recupera `Processing`** → o item fica órfão para sempre: não conta em `SentCount`, não é retriado, invisível. Se o provider já tinha aceitado o envio, o email FOI entregue mas o sistema não sabe; se não tinha, o email está **perdido**. Hospedagem compartilhada recicla pools com frequência — isso **vai** acontecer.

### P0-4 — Breaker por provider (Sistema A) é *latching*: abre e nunca fecha [FATO]
`EmailProviderCircuitBreaker`: `RecordSuccess` só alimenta a janela — **não há código que feche um breaker aberto**, não há half-open, não há cooldown, e a interface (`IProviderCircuitBreaker.cs`, 8 linhas) **não tem `Reset`** — nenhum endpoint fecha esse breaker (o `POST /pilot/reset` fecha só o `PilotCircuitBreaker` do Sistema B). Única "recuperação": recycle do processo (por ser estado in-memory). Combinado com o classificador (abaixo), um único erro pode desligar um provider até o próximo deploy.

### P0-5 — Classificador trata "bounce" como erro crítico de autenticação [FATO]
`EmailErrorClassifier.IsCriticalAuthError` inclui a substring `"bounce"` (`EmailErrorClassifier.cs:12`). Um hard bounce é problema do **destinatário**, não do provider. Efeito:
- Sistema A: 1 erro contendo "bounce" → breaker do provider **aberto para sempre** (P0-4).
- Sistema B: 1 erro contendo "bounce" → `PilotCircuitBreaker.Open()` imediato → **TODAS as campanhas bloqueadas** até reset manual de admin. Isso explica o padrão operacional atual de rodar `ensure_breaker_closed()` antes de cada wave — o sistema está tratando o sintoma via script.
Também: matching por substring (`"401"`, `"apikey"`) é frágil — qualquer mensagem/ID contendo esses fragmentos dispara.

### P0-6 — Idempotência com corrida (TOCTOU) + `InFlight` órfão bloqueia a chave por 24h [FATO]
- `IX_EmailSendLog_IdempotencyKey` **não é único** (`EmailSendLogConfiguration.cs:29-30`); o fluxo é check-then-insert sem lock (`EmailFallbackOrchestrator.cs:44-76`). Duas requests simultâneas com a mesma chave passam ambas pelo check → **envio duplicado**.
- Se o processo morre com o log em `InFlight`, toda nova tentativa com a mesma chave recebe **409 por 24h** — sem expiração de `InFlight` órfão, sem recovery.
- No hard-timeout, o log é marcado `Failed` mesmo que o provider possa ter aceitado o envio (resultado ambíguo). O status `"Uncertain"` **existe** no domínio (`EmailSendLog.cs:32`) mas **nunca é usado** — cliente que retenta após timeout pode duplicar.

### P1-7 — Round-robin legado distribui para providers mortos; retry não troca provider [FATO]
Ver seção 4. `(EmailProvider)(providerIndex % 6)` (`EmailMarketingService.cs:468`) + `Requeue` mantém `AssignedProvider` (`EmailQueueItem.cs:95-102`) → item atribuído a MailerSend/ElasticEmail falha 3× e é abandonado. Sem dead-letter, sem realocação, sem alerta.

### P1-8 — PilotCircuitBreaker é GLOBAL: um provider ruim bloqueia todos [FATO]
A janela de 10 amostras não tem dimensão de provider (`PilotCircuitBreaker.cs:18`). Com 1 provider 100% falho no mix, a taxa passa de 30% rápido e **todo o envio de campanhas para**. Reset apenas manual.

### P1-9 — Worker não para quando o breaker abre no meio do ciclo [FATO]
`EmailQueueProcessorWorker.cs:218-235` detecta a abertura, **loga e continua** processando o restante do lote e os providers seguintes. O bloqueio só vale no próximo ciclo. Num ciclo com 120 itens (6×20), dezenas de envios podem sair APÓS o trip.

### P1-10 — Estado de proteção 100% in-memory em hospedagem compartilhada [FATO / HIPÓTESE de frequência]
`ProviderQuotaGuard`, `EmailProviderCircuitBreaker` e `PilotCircuitBreaker` são singletons em memória. Recycle do app pool (rotina no SmarterASP) **zera quotas e fecha/abre breakers**: limites diários podem ser ultrapassados silenciosamente (contadores voltam a 0 no meio do dia). Além disso, **o worker (B) não consulta o `ProviderQuotaGuard` (A)** — são dois sistemas de rate-limit desconectados; os limites por provider do `EmailChain` não valem para campanhas (só limites globais Daily/Hourly + batch/ciclo).

### P1-11 — Hard timeout único aborta a cadeia inteira [FATO]
Um provider lento consome os 60s e o `break` impede os demais (`EmailFallbackOrchestrator.cs:126-134`). Sem timeout por provider, um Brevo pendurado derruba o request inteiro mesmo com 5 alternativas saudáveis.

### P1-12 — Contadores de campanha inconsistentes [FATO]
- Retry bem-sucedido após falha: mesmo destinatário incrementa `FailedCount` **e** `SentCount` → `Sent+Failed > TotalRecipients`, distorcendo `CheckCompletion` e analytics.
- Webhooks de **bounce/unsubscribe/spam sem idempotência** (`BrevoWebhookController.cs:328, 376`) — reentrega conta em dobro. Click usa scan de string no AuditLog como "idempotência" (frágil e caro).
- `EmailSentCount` do Customer conta **enfileiramento**, não envio (semântica OK para dedup, mas o nome mente para relatórios).

### P1-13 — Concorrência sem constraint no banco [FATO no código / HIPÓTESE no efeito]
Dedup de enfileiramento é read-then-insert em memória, **sem `UNIQUE (campaign_id, customer_id)`**. Duas chamadas concorrentes de queue para a mesma campanha duplicam destinatários. Se o worker um dia rodar em 2+ instâncias (web garden/scale-out), o fetch de `Queued` sem claim atômico causa **envio duplicado** — hoje é instância única [HIPÓTESE dependente do hosting].

### P1-14 — Índices ausentes em hot paths [FATO]
- `email_queue_items.provider_message_id` sem índice → **table scan a cada webhook** Brevo (delivered/opened/click).
- `email_suppressions` sem NENHUM índice → scan por destinatário no loop de enfileiramento.
- `(status, assigned_provider, scheduled_at)` melhoraria a query do worker (6×/ciclo).

### P1-15 — Zero testes na implementação real do fallback [FATO]
`EmailFallbackOrchestrator`: **0 testes** (os 7 `EmailDispatchContractTests` mockam a própria interface — testam o mock). Sem testes: gate Tier2, skip por breaker/quota, idempotência real, timeout, persistência. Os 6 senders HTTP: 0 testes (parse de resposta, mapeamento de erro). `EmailProviderCircuitBreaker`: 0 testes. Worker: 0 testes. O fix do PR #31 **não tem rede de proteção** — um refactor pode removê-lo sem nenhum teste falhar.

### P2 — Menores
- `ProviderHint` aceito no payload e **ignorado** pelo orquestrador (parâmetro morto) (`IntegrationsController.cs:193`). [FATO]
- `cta_link` hardcoded para `https://diaxcrm.com.br/landing/...` — **mesmo domínio morto** do unsubscribe (`EmailMarketingService.cs:889`). [FATO]
- Tabela `email_events` mapeada e **nunca gravada** (schema morto). `EmailCampaignStatus.Failed` sem transição. [FATO]
- `ValidateReadiness` permite envio real em `Development` e `Test` (`:1220-1225`) — sem sandbox mode. [FATO]
- `Requeue()` apaga `LastError` — perde histórico de falha. [FATO]
- Gate de UTM é por substring única no corpo (um link com UTM libera todos). [FATO]
- Workflow de deploy não emite a seção `EmailChain` no `appsettings.Production.json` gerado — produção depende do `appsettings.json` base (risco de drift dev/prod). [FATO]
- Quota consumida antes do envio e não devolvida em falha (queima crédito de provider que falhou). [FATO]

---

## 7. Melhorias Recomendadas

### P0 — corrigir antes de qualquer nova campanha
| # | Ação | Esforço |
|---|---|---|
| 1 | **Corrigir `unsubscribe_url`**: gerar `https://api.alexandrequeiroz.com.br/unsubscribe?token=<HMAC>` usando o mesmo HMAC do `EmailUnsubscribeController` (payload `unsub:{userId}:{email}`); centralizar num `IUnsubscribeLinkBuilder`; corrigir também `cta_link`. Base URL via config, nunca hardcode | 2-4h |
| 2 | **Adicionar checks de `EmailOptOut` + `IsSuppressedAsync` em `QueueWithSmartAssignmentAsync`** (copiar do caminho legado, `:450-460`) | 1h |
| 3 | **Recovery de `Processing` órfão**: no início de cada ciclo do worker, `Processing` com `ProcessingStartedAt < now-30min` → volta para `Queued` (ou `Failed` se `AttemptCount >= 3`); logar como `Uncertain` quando houver risco de duplicidade | 2h |
| 4 | **Fechar o breaker do Sistema A**: implementar half-open com cooldown (ex.: 5 min aberto → 1 tentativa de prova) OU no mínimo `Reset(providerKey)` + endpoint admin | 3h |
| 5 | **Remover `"bounce"` de `IsCriticalAuthError`** — bounce é evento de destinatário; tratar via opt-out/supressão (webhook já faz). Manter crítico só para 401/unauthorized/invalid api key | 30min |
| 6 | **Idempotência robusta**: índice único filtrado em `IdempotencyKey` (`WHERE idempotency_key IS NOT NULL AND status IN ('InFlight','Sent')`) + capturar violação como `Duplicate/InProgress`; expirar `InFlight` > 10min (tratar como `Uncertain`) | 3h |

### P1 — próxima sprint
| # | Ação |
|---|---|
| 7 | Retry do worker com **realocação de provider** (excluir provider que falhou; escolher entre saudáveis via `IProviderHealthService`, que já existe em `Pro/`) e **remover providers sem credencial da rotação** (feature flag `Enabled` por provider em config — desligar MailerSend/ElasticEmail até ter credencial) |
| 8 | `PilotCircuitBreaker` **por provider** (ou pelo menos janela por provider) + auto-close com half-open; parar o ciclo do worker imediatamente quando abrir (`break` no loop) |
| 9 | **Timeout por provider** (ex.: 15s) dentro do budget global de 60s no orquestrador |
| 10 | **Persistir quota e breaker** (tabela pequena ou reaproveitar `CountSentByProviderSinceAsync` que já existe) para sobreviver a recycle; fazer o worker respeitar `EmailChain:ProviderDailyLimits` |
| 11 | **`UNIQUE (campaign_id, customer_id)`** em `email_queue_items` (filtrado por `customer_id IS NOT NULL`) + tratamento de violação como skip |
| 12 | Índices: `provider_message_id`, `email_suppressions (user_id, email)` e `(user_id, domain_pattern)`, `(status, assigned_provider, scheduled_at)` |
| 13 | Idempotência de webhook bounce/unsubscribe (guarda por `ProviderMessageId`+evento; a tabela morta `email_events` é o lugar natural — passa a ser o ledger de eventos com índice único) |
| 14 | Suite de testes do orquestrador real + PR #31 + worker (lista na seção 10) |
| 15 | Honrar ou remover `ProviderHint`; separar contadores de retry (não duplicar Sent+Failed — decrementar Failed no requeue ou contar por estado final) |

### P2 — evolução
- **Outbox pattern** no Sistema A (hoje é síncrono: request HTTP segura a cadeia toda): gravar intenção → worker despacha → status consultável. Unifica com o Sistema B.
- **Dead-letter queue** explícita (status `DeadLetter` + tela/endpoint de reprocessamento) em vez de `Failed` terminal invisível.
- **Sandbox mode**: `Email:SandboxMode=true` fora de produção → redireciona tudo para caixa de teste (hoje Development envia de verdade).
- **Alertas Telegram** (bot já existe: `@alexandrequeiroz_marketing_bot`): breaker aberto, AllFailed, quota 80%, itens em DLQ.
- **Dashboard operacional**: `GET /email-quota` já existe; expor breaker states + fila por status + últimos AllFailed.
- **Logs estruturados** com `RequestId` correlacionando controller→orquestrador→sender (hoje há logger, mas sem correlação de ponta a ponta).
- **Validação de template** no save da campanha (tokens desconhecidos, links http://, imagens quebradas) — hoje só valida unsubscribe/UTM no queue.
- Apagar `email_events` OU adotá-la como ledger (não deixar schema morto); remover `EmailCampaignStatus.Failed` se confirmado morto.

---

## 8. Plano de Implementação Sugerido

**Fase 1 — Estancar (1 dia):** P0-1, P0-2, P0-5 (uma linha), item 7 parcial (desligar MailerSend/ElasticEmail da rotação via config). Deploy. *Sem migration.*

**Fase 2 — Não perder email (1-2 dias):** P0-3 (recovery de Processing), P0-6 (índice único + InFlight expiry — 1 migration), P0-4 (reset/half-open do breaker A). Testes de regressão de cada um.

**Fase 3 — Robustez (3-5 dias):** P1-7/8/9/10/11/12 + migration de índices + suite de testes (seção 10). `/wave-qa` completo ao final (protocolo obrigatório do projeto).

**Fase 4 — Arquitetura (contínuo):** outbox unificado, DLQ, sandbox, alertas, dashboard.

---

## 9. Arquivos Que Precisam Mudar

| Arquivo | Mudança |
|---|---|
| `Diax.Application/EmailMarketing/EmailMarketingService.cs` | P0-1 (unsubscribe/cta builder), P0-2 (opt-out/suppression no smart assignment) |
| `Diax.Application/EmailMarketing/EmailErrorClassifier.cs` | P0-5 (remover "bounce") |
| `Diax.Application/EmailMarketing/Dispatch/EmailFallbackOrchestrator.cs` | P0-6 (idempotência), P1-9 (timeout por provider), `Uncertain` no timeout |
| `Diax.Application/EmailMarketing/Dispatch/IProviderCircuitBreaker.cs` + `Diax.Infrastructure/Email/EmailProviderCircuitBreaker.cs` | P0-4 (half-open/reset) |
| `Diax.Infrastructure/Email/EmailQueueProcessorWorker.cs` | P0-3 (recovery Processing), P1-8 (break no trip), P1-7 (realocação de provider), P1-10 (respeitar limites por provider) |
| `Diax.Infrastructure/Data/Repositories/EmailQueueRepository.cs` | query de recovery `Processing`, claim atômico |
| `Diax.Domain/EmailMarketing/EmailQueueItem.cs` | `ReassignProvider()`, preservar `LastError` no requeue |
| `Diax.Infrastructure/Data/Configurations/EmailSendLogConfiguration.cs` + nova migration | índice único filtrado de idempotência |
| Nova migration | UNIQUE (campaign_id, customer_id); índices provider_message_id, suppressions, (status, assigned_provider, scheduled_at) |
| `Diax.Api/Controllers/V1/BrevoWebhookController.cs` | idempotência bounce/unsubscribe/click |
| `Diax.Api/appsettings.json` + `deploy-api-core-smarterasp.yml` | flag `Enabled` por provider; emitir `EmailChain` no Production gerado |
| `Diax.Api/Controllers/V1/EmailProvidersController.cs` (ou Pilot) | endpoint de reset/estado do breaker por provider |

---

## 10. Testes Que Precisam Ser Criados

**Orquestrador real (`EmailFallbackOrchestrator` com senders fake keyed em DI):**
1. Provider A falha → B envia (fallback real, attempts=2).
2. Todos Tier1 falham + `allowUnaligned=false` → `AllFailed`, Tier2 nunca chamado.
3. `allowUnaligned=true` → Tier2 usado e flagado no log.
4. Breaker aberto para A → pula direto para B sem consumir quota de A.
5. Quota esgotada de A → pula para B.
6. Timeout: provider pendurado → hoje aborta a cadeia (documentar) → após fix, pula para o próximo.
7. Idempotência real: 2 dispatches concorrentes mesma chave → exatamente 1 envio (exige o índice único).
8. `InFlight` órfão >10min → nova tentativa não recebe 409 eterno.

**Worker:**
9. Item `Processing` órfão é recuperado no ciclo seguinte.
10. Breaker abre no meio do lote → ciclo para imediatamente.
11. Retry realoca provider após falha; provider desabilitado nunca recebe item.
12. Retry bem-sucedido não deixa `Sent+Failed > TotalRecipients`.

**Serviço:**
13. `QueueWithSmartAssignmentAsync` pula opt-out e suprimidos (regressão P0-2).
14. `QueueWithSmartAssignmentAsync` + `QueueCampaignRecipientsAsync` chamam `RegisterEmailSent` (regressão PR #31).
15. `unsubscribe_url` gerada aponta para o host de produção com token HMAC válido que o `EmailUnsubscribeController` aceita (round-trip).
16. Provider string desconhecida em `assignedProvider` → rejeitar (ou testar o default Brevo se mantido — decisão explícita).

**Classificador / breakers:**
17. `"bounce"` NÃO abre breaker (regressão P0-5); 401/invalid api key abre.
18. `EmailProviderCircuitBreaker`: half-open fecha após sucesso de prova; `Reset(key)` funciona.
19. Concorrência: 20 threads em `TryConsume` com limite N → nunca excede N.

**Webhooks:**
20. Bounce/unsubscribe reentregues 2× → contadores incrementam 1×.

---

## 11. Recomendações de Arquitetura Futura

1. **Unificar os dois sistemas**: o Sistema B deveria enfileirar e o worker despachar **através do orquestrador** (Sistema A), herdando fallback, quota, breaker por provider e log por tentativa. Hoje a lógica boa existe mas as campanhas — o volume real — não passam por ela.
2. **Outbox + despacho assíncrono** também para o Sistema A: request grava intenção e retorna 202 com `requestId`; worker único despacha. Elimina o 60s síncrono e o estado `InFlight` ambíguo.
3. **Estado durável de proteção**: quota/breaker no banco (SQL Server já disponível). Em shared hosting com recycle, singleton in-memory é ilusão de proteção.
4. **Health-driven routing**: `IProviderHealthService`/`SmartPreselectionService` já existem — usá-los no servidor (worker/orquestrador) em vez de confiar no cliente para escolher provider saudável.
5. **Ledger de eventos único** (`email_events` ressuscitada, com unique key por evento) alimentando contadores por agregação — elimina as classes de dupla contagem.
6. **Config por ambiente com sandbox default** fora de produção.

---

## 12. Veredito: pronto para produção?

**NÃO — com ressalva.** O sistema *está* em produção e funciona no caminho feliz (histórico: centenas de envios com 0 falhas por wave quando os scripts evitam os providers mortos e resetam o breaker). Mas para o padrão "pronto para produção" a resposta é objetiva:

1. **Compliance quebrada em 100% dos emails de campanha**: link de unsubscribe morto (P0-1) + caminho principal de enfileiramento ignora opt-out/supressão (P0-2). Qualquer um dos dois, sozinho, já é bloqueante para email marketing.
2. **Pode perder email silenciosamente**: `Processing` órfão sem recovery (P0-3) e itens atribuídos a providers mortos abandonados após 3 retries no mesmo provider (P1-7) — sem DLQ, sem alerta.
3. **A resiliência anunciada não se sustenta sem intervenção manual**: breaker do Sistema A nunca fecha (P0-4), breaker global do B abre com 1 bounce e exige admin (P0-5/P1-8), quota zera a cada recycle (P1-10). A operação atual só é estável porque **scripts externos** contornam (escolhem providers, resetam breaker) — o sistema não se defende sozinho.
4. **Zero testes na lógica de fallback real** (P1-15): nenhuma mudança nesses arquivos é segura hoje.

**Critério de saída para "pronto"**: Fases 1 e 2 do plano (seção 8) concluídas + testes 1-8 e 13-17 verdes. Estimativa: 2-3 dias de trabalho focado.

---

## 13. Status de Implementação (2026-07-01, branch `fix/email-audit-p0-p1`)

**Todas as correções P0 e P1 foram implementadas no mesmo dia da auditoria** — commit local na branch `fix/email-audit-p0-p1` (aguardando autorização de push; push em `main` dispara auto-deploy + auto-migrate).

| Achado | Status | Onde |
|---|---|---|
| P0-1 unsubscribe/CTA mortos | OK | `UnsubscribeLinkBuilder` (HMAC + `EmailLinks:PublicBaseUrl`); controller delega ao mesmo algoritmo |
| P0-2 opt-out/supressão no smart assignment | OK | checks adicionados (+ em send-bulk e send-single) |
| P0-3 Processing órfão | OK | `RecoverStaleProcessingAsync` no ciclo (>30min -> requeue ou Failed) |
| P0-4 breaker A latching | OK | half-open (cooldown 5min, 1 prova) + `Reset` + endpoints `/email-providers/breaker-status` e `/email-providers/breaker/reset` |
| P0-5 "bounce" crítico | OK | removido do classifier; PilotCircuitBreaker passou a usar o classifier compartilhado |
| P0-6 idempotência TOCTOU + InFlight órfão | OK | índice único filtrado + `TryCreateInFlightAsync` (corrida->InProgress) + InFlight >10min->`Uncertain` + `RecordAttempt` não flipa status |
| P1-7 providers mortos na rotação / retry sem troca | OK | `IEmailProviderPolicy` + `Email:DisabledProviders` (mailersend/elasticemail) + reassinalação no retry + resgate de itens órfãos + parse estrito (typo não cai mais em Brevo) |
| P1-8 pilot global / worker não para | OK | worker usa breaker POR provider + ciclo para imediatamente no trip global |
| P1-9 timeout único | OK | `PerProviderTimeout` (20s) com continue; budget esgotado -> `Uncertain` (não Failed) |
| P1-10 quota in-memory | OK | hidratação do banco no cold start (`DbProviderQuotaUsageSource`) + worker respeita `ProviderDailyLimits` |
| P1-12 contadores | OK | `DecrementFailed` no requeue; webhooks bounce/unsub/click idempotentes via ledger `email_events` (tabela morta ressuscitada); hard_bounce não infla mais UnsubscribeCount |
| P1-13/14 índices | OK | migration `EmailAuditHardening`: UX idempotency, UX (campaign,customer,Queued) com pré-limpeza de duplicatas, provider_message_id, suppressions, (status,provider,scheduled_at), email_events |
| P1-15 testes | OK | **55 novos testes** (orquestrador real, breaker half-open, cycle processor, smart assignment, unsubscribe round-trip, ledger de webhook) — suite completa 431/431 verde |
| P2 sandbox / ProviderHint / LastError | OK | `Email:SandboxRedirectTo`; hint honrado (frente do próprio tier); Requeue preserva LastError |

**Pendências para o deploy (decisão do usuário):**
1. Autorizar push da branch -> PR -> merge em `main` (auto-deploy + migration automática).
2. Conferir `EmailLinks:PublicBaseUrl` e `DefaultCtaUrl` (defaults: api./www.alexandrequeiroz.com.br).
3. Reativar `mailersend`/`elasticemail` em `Email:DisabledProviders` apenas quando houver credencial válida.
4. Scripts `fire_w*`: nomes de provider inválidos agora são REJEITADOS (antes caíam em Brevo) e opt-out/suprimidos são pulados — o `SkippedCount` pode aumentar (comportamento correto).

*Auditoria gerada por Claude Code em 2026-07-01. Fatos citados com arquivo:linha verificados no commit `db16787` (main). Estado operacional dos providers baseado nas sessões de 2026-05-16 a 2026-06-27 — revalidar credenciais antes da Fase 1.*
