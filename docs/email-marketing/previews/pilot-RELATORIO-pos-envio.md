# Relatório pós-envio — Piloto AQ · Restaurantes · Criação de Sites

**Data/hora do enqueue:** 2026-06-16T19:37:42-03:00 (effectiveScheduledAt: 2026-06-16T22:37:45Z)
**campaignId:** 94cb0f7e-e0bb-42c9-a955-aa504b781153
**Remetente:** contato@alexandrequeiroz.com.br / Alexandre Queiroz Marketing Digital
**Assunto:** Posso fazer um diagnóstico rápido do site do {{empresa}}?

## Resultado do enqueue
- requestedCount: 29
- **queuedCount: 29**
- skippedCount: 0 (nenhuma supressão/opt-out no momento do enqueue)
- skippedRecipients: []

## Gates pré-envio (todos OK)
- GATE1 draft + sent=0: ✅
- GATE2 copy íntegra ({{unsubscribe_url}} + WhatsApp + site): ✅
- GATE3 29 válidos / únicos / 0 opt-out / 0 churned: ✅

## Estado inicial (logo após enqueue)
- Campanha status: 2 (Processing) · totalRecipients: 29
- sentCount: 0 · deliveredCount: 0 · openCount: 0 · clickCount: 0 · bounceCount: 0 · unsubscribeCount: 0 · failedCount: 0
- Recipients: 29 em status Queued(0) · 0 duplicados · 0 com erro
- (worker dispara a cada ~5 min, batch 20/provider — envio real nos próximos ciclos)

## Critérios de parada — status
Nenhum acionado: 0 erro de provider · 0 bounce · 0 duplicado · 0 falha de unsubscribe/tracking.

## Observações / ressalvas
- Não foi possível verificar PROGRAMATICAMENTE se o teste interno (admin@) caiu em inbox vs spam — usuário autorizou o envio mesmo assim.
- 17 dos 29 já haviam recebido email antes (decisão do usuário: re-contato rastreado).
- Tagueamento (aq-piloto-restaurante etc.) permanece DEFERIDO (sem endpoint aditivo seguro).
- Arquivos: previews/pilot-eligible-customer-ids.json, previews/pilot-preflight.json (valid/blocked), templates/pilot-restaurante-sites.html + .txt

## Monitoramento — 2026-06-16 ~19:50 (-03:00)
- campaign.status: 3 (Completed) · sentCount: **19** · deliveredCount: **3** (webhook Brevo) · failedCount: **10**
- recipients: 19 Sent · 10 Queued (retry) · 0 com lastError visível · **0 duplicados**
- bounces: **0** · unsubscribes: **0** · spam: **0** · opens/clicks: 0 (cedo)
- Provider health: Brevo/Mailjet/Resend/ElasticEmail **health=ok, failedToday=0**, capacidade sobrando

### Diagnóstico do failedCount=10
- Os 10 não-enviados tinham **provider=None** → **não foram rejeitados por provedor** (failedToday=0 em todos).
- Falha = **atribuição de provedor sob rajada** no 1º ciclo; worker fez `RequeuePendingRetries` (backoff 2^attempt×15min ≈ 30min).
- **NÃO é problema de domínio/spam/conteúdo** (0 bounce, 0 spam, entregas confirmadas).

### Critério de parada acionado
- **Failed rate 1ª tentativa = 10/29 = 34% (>5%)** → escalada PAUSADA.
- Demais gates OK: bounce 0%, unsub 0%, spam 0, duplicados 0.

### Decisão
- **PAUSAR escalada.** Não iniciar novo nicho/serviço.
- Deixar o **retry automático** (~30 min) tentar os 10 — SEM reenvio manual (rule 8).
- Re-checar após o ciclo de retry para confirmar se os 10 são enviados.

### Próximo passo
- Re-checar métricas em ~30–40 min (retry dos 10 + opens/clicks). Plano de escala em `pilot-PLANO-ESCALA.md` (preparado, NÃO enviar).

## Tentativa de Opção A (desabilitar ElasticEmail+MailerSend) — 2026-06-16 ~20:33
**Resultado: NÃO executável com o acesso disponível. Nenhuma config alterada.**
- `ProviderMap` é **hardcoded** (EmailQueueProcessorWorker.cs:26-34) com os 6 providers. Não há flag Enabled em appsettings/env/DB acessível via API. Desabilitar exige editar código/credenciais no servidor DIAX + redeploy (sem acesso).
- Itens estão **pré-atribuídos no DB** ao provider (GetPendingBatchByProviderAsync). Os 10 estão fixados em ElasticEmail/MailerSend; desabilitar deixaria órfãos, não redirecionaria.
- `POST /queue-with-assignment` **cria novos itens** → duplicaria os 10 (viola não-duplicar). Não usado.
- Snapshot de reversibilidade: `pilot-providers-config-snapshot.json`.

### Correção real (requer acesso ao servidor DIAX / deploy)
1. No ambiente DIAX prod: **adicionar credenciais válidas** de ElasticEmail (ApiKey) e MailerSend (ApiToken) — OU
2. **Remover ElasticEmail/MailerSend do `ProviderMap`** (EmailQueueProcessorWorker.cs) e redeploy — OU
3. Deixar os 10 esgotarem o retry (ficam Failed, nunca enviados) e **re-enfileirá-los depois** que os providers estiverem ok (sem duplicar — nunca receberam).
- Recomendado: (1) se as chaves existem (estão no `.secrets.env` local), basta injetá-las no env do DIAX prod.
