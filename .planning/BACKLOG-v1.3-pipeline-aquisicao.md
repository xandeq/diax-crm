# Milestone v1.3 (proposto) — Pipeline de Aquisição

> **Status: 🔵 Proposto, não iniciado.** Escrito em 2026-09-05 a partir do trabalho real da sessão
> (não é mapeamento especulativo — cada item cita a evidência que o gerou). **Não mexe em
> `STATE.md`/`ROADMAP.md`** — aqueles pertencem ao milestone v1.2 (Agentes de IA), hoje **pausado**
> no meio da Phase 02 por causa de outra sessão mexendo no mesmo repo. Quando o usuário decidir
> começar este milestone, rodar `/gsd:new-milestone` (ou promover este arquivo a `ROADMAP.md` na
> hora certa) — não antes, para não colidir com o v1.2 pausado.

## Goal

Pipeline extração→CRM→email→WhatsApp já funciona e está em produção (branch
`chore/email-automation-versioned`, PRs #95-#97 merged). Este milestone não é "fazer funcionar" —
é fechar os gaps que sobraram: qualidade de dado na entrada, rastreabilidade end-to-end, escala
seguindo deliverability, e o canal WhatsApp que está construído mas nunca rodou de verdade.

## Non-goals (out of scope)

- Agentes de IA (v1.2, já em andamento, não mexer)
- Trocar de provedor de email/WhatsApp
- Qualquer coisa que precise de mais volume de envio ANTES do SPF estabilizar (~11-18/09)

---

## Phase A — Extração: qualidade na origem

**Por que**: o filtro geo (state=SP em lote "grande_vitoria_es") e o `.ch` (Globus Karriere suíço)
só foram pegos porque eu testei ao vivo — o worker .NET não tem o mesmo nível de verificação que
a ponte Python acumulou em 6 incidentes (extrator-diax, 22/08→30/08).

| # | Item | Evidência | Esforço |
|---|---|---|---|
| A1 | Verificação MX/domínio (não só email-lixo conhecido) no `ExtractorIntegrationService.cs` — hoje só existe em `mx_check.py` (ponte manual) | bounce 7,5% na semana de 03-04/09 antes do MX check | M |
| A2 | Avaliar Google Places API como fonte alternativa/complementar (endereço+telefone+rating verificados, vs. scraping de página) | scraping devolve `city: "Casas de Carne em Campinas"` como nome de cidade — dado sujo na fonte | G |
| A3 | Dashboard/log de taxa de rejeição por motivo (geo, email-lixo, duplicado) ao longo do tempo | hoje só sai no stdout do script manual; sem histórico | P |
| A4 | Aplicar `classify_url` (directory vs site próprio) do `site_check.py` como sinal de qualidade NO IMPORT, não só na personalização do email | lead com `website` = página do econodata/cliniguia é tratado igual a site próprio hoje no scoring | P |

## Phase B — Import CRM: rastreabilidade e dedup

**Por que**: dedup por email quebra quando o mesmo negócio muda de email entre passadas do scraper
(recomendação já registrada em 29/08, nunca aplicada); `/customers/import` não dedupa de verdade
p/ `source=Scraping` (bug que a ponte Python contorna manualmente há 3 semanas).

| # | Item | Evidência | Esforço |
|---|---|---|---|
| B1 | `Customer.ExternalId` = `lead.Id` do extrator + índice único (migration EF) | recomendação de [[extrator-crm-pull-integration-2026-08-29]], nunca feita | M |
| B2 | Dedup real no `/customers/import` para `source=Scraping` (hoje só funciona pra `source=Import`) | incidente 30/08 (205 leads lixo); ponte Python faz dedup client-side como workaround | M |
| B3 | Calcular `lead_score` inicial no momento do import, não só no worker diário 06h BRT | até 24h de atraso entre lead entrar e primeira pontuação | P |

## Phase C — Email: escalar com segurança

**Por que**: SPF/DMARC corrigidos 04/09 (21→7 lookups, pct=25); volume hoje é 10% da capacidade;
A/B de subject existe mas ninguém nunca leu o resultado.

| # | Item | Evidência | Esforço | Depende de |
|---|---|---|---|---|
| C1 | DMARC pct=25→100 | roadmap 03/09, decisão adiada 1 semana de propósito | P | ~11/09 |
| C2 | Volume 87→300/dia, warmup gradual +50/semana | `OTIMIZACAO-FUNIL-2026-09-03.md` P4 | M | C1 + bounce <3% por 1 semana |
| C3 | Fechar o loop do A/B de subject (weekly_report não segrega opens por variante A/B) | `SUBJ_B`/`ab_flip` existem, resultado nunca medido | P | — |
| C4 | Aplicar `niche_perf.py` (kill/scale) também ao ÂNGULO de copy dentro do nicho, não só ao nicho | hoje só nicho inteiro liga/desliga | M | volume maior p/ significância |
| C5 | Testar os 560 leads "webmail" (gmail/hotmail comercial) com cap pequeno + tag própria | hoje 100% ignorados pelo filtro corporativo; PME BR usa gmail como email de negócio | P | C1 |

## Phase D — WhatsApp: sair do zero

**Por que**: todo o pipeline foi construído hoje (endpoint `whatsapp-event`, export segmentado,
handoff, workflow n8n) mas **zero mensagens foram enviadas** — `whats_app_sent_count=0` em
todos os 7.998 leads.

| # | Item | Evidência | Esforço |
|---|---|---|---|
| D1 | Confirmar que a outra sessão (WAHA local) importou o `n8n-wa-outreach-1a1.json` e rodou o teste com CAP=1 | handoff entregue 05/09, execução não confirmada | — (checar) |
| D2 | Adicionar o IF no bot inbound (`8nKTlX4Y7Hy1EUEn`) que desvia lead de prospecção pra humano em vez da IA responder | risco identificado no handoff, ainda não implementado | P |
| D3 | Métricas de WhatsApp (equivalente ao `weekly_report` de email): enviados/respondidos/opt-out por semana | nenhum relatório recorrente existe pro canal | P |
| D4 | Escalar `wa_es` (480 leads) → `wa_br` (1138) só depois de validar taxa de resposta em ES | regra de segurança do handoff (1-a-1, ban do número é risco real) | — (decisão, não código) |

## Phase E — Observabilidade / risco operacional (cross-cutting)

| # | Item | Evidência | Esforço |
|---|---|---|---|
| E1 | Ponto único de falha: waves, vigia de respostas, n8n e WAHA rodam só no notebook (Task Scheduler) — notebook desligado = pipeline inteiro para | nenhuma redundância; já aconteceu com a VPS antes (várias suspensões) | G |
| E2 | Prova social real no `/portfolio` — cases hoje parecem placeholder ("Academia FitPower") | achado do subagente de copy 03/09, nunca resolvido | — (decisão do usuário) |

Esforço: P=pequeno (<1h) · M=médio (sessão) · G=grande (dias/decisão arquitetural)

---

## Coverage rápida (o que já está PRONTO, não repetir)

✅ P1b (progressão de status), waves tolerantes a atraso, FUP2/3 com diagnóstico real, MX check
(lado Python), filtro geo (lado .NET e Python), lead scoring rebalanceado, endpoint
`whatsapp-event`, `ExtractorPullWorker` ligado em prod, SPF/DMARC corrigidos, purge de `audit_logs`.

## Quando ativar este milestone de verdade

1. Confirmar D1 (zero-custo, é só perguntar/checar).
2. Rodar `/gsd:new-milestone` quando o v1.2 (Agentes de IA) for retomado/concluído ou explicitamente
   pausado por decisão (hoje está pausado por acidente de sessão concorrente, não por escolha).
3. Ou, se o usuário quiser rodar em paralelo ao v1.2: promover este arquivo direto a plano de
   execução (`/gsd:plan-phase` apontando pra Phase A, B, C ou D) sem mexer no `STATE.md` do v1.2.
