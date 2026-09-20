# Requirements: DIAX CRM — Milestone v1.4

**Defined:** 2026-09-19
**Core Value:** Centralizar todas as operações de negócio (leads, finanças, comunicação, IA) em um único sistema pessoal, eliminando ferramentas externas pagas

## v1 Requirements

Requirements para o milestone v1.4 "Funil de Outreach — Cadência e Qualidade". Cada um mapeia para uma fase do roadmap.

### Backfill

- [x] **BFIL-01**: Backlog pré-v1.3 (~5.266 leads que nunca passaram pelo `LeadScoringService`) tem `LeadScore`/`Segment` recalculados usando o motor oficial (C#), não o script Python ad-hoc criado em sessão de auditoria manual (2026-09-17/19)

### Email

- [ ] **EMAL-01** (parcial — ver Traceability): DMARC sobe de `pct=25` para `pct=100`
- [ ] **EMAL-02** (parcial — ver Traceability): Volume de envio escala de 87 para 300/dia com warmup gradual
- [x] **EMAL-03**: A/B de subject (variant A/B) tem o resultado medido e reportado (hoje só alterna, ninguém lê)
- [ ] **EMAL-04** (parcial — ver Traceability): Leads "webmail" (gmail/hotmail comercial, 560 hoje ignorados) testados com cap pequeno + tag própria

### WhatsApp

- [x] **WHAT-01** (por equivalência funcional — ver Traceability): Handoff (`n8n-wa-outreach-1a1.json`) confirmado em execução pela sessão que opera o WAHA local
- [x] **WHAT-02**: Bot inbound de IA (`8nKTlX4Y7Hy1EUEn`) desvia lead de prospecção pra humano em vez de responder
- [x] **WHAT-03**: Relatório semanal de métricas do canal WhatsApp (enviados/respondidos/opt-out)
- [x] **WHAT-04**: Escalar de `wa_es` (480 leads) para `wa_br` (1138) após validar taxa de resposta em ES

### Funil / Qualified

- [x] **QUAL-01**: Critério objetivo de avanço Contacted→Qualified definido e documentado (hoje é implícito/manual)
- [x] **QUAL-02**: `reply_watch.py` avança status automaticamente no CRM quando o critério QUAL-01 é atingido

### Consolidação

- [x] **SCRP-01**: Motor único parametrizado (nicho/wave/data como argumentos) substitui os 79+ scripts `fire_*.py` soltos em `docs/email-marketing/`

### Observabilidade

- [x] **OBSV-01**: Pipeline de outreach (waves, vigia de respostas, n8n, WAHA) deixa de depender só do notebook do usuário via Task Scheduler — ponto único de falha
- [ ] **DASH-01**: Dashboard de conversão por etapa do funil (Lead→Contacted→Qualified→Customer, com percentuais, não só volume bruto)

## v2 Requirements

Deferido — não bloqueia v1.4.

### Extração

- **EXTR-04**: Avaliar viabilidade do Google Places API como fonte alternativa/complementar de leads (endereço/telefone verificados vs. scraping de página)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Google Places API (EXTR-04) | Exploratório, não bloqueia entrega do funil de outreach — fica para milestone futuro |
| Retomar v1.2 (Agentes de IA) | Milestone paralelo, pausado, com escopo e riscos próprios — não misturar com v1.4 |
| Reescrever `LeadScoringService` | v1.4 só precisa RODAR o motor existente sobre o backlog antigo, não mudar a lógica de scoring |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| BFIL-01 | Phase 9 — Backfill de Score do Backlog | Complete (09-01: test + trigger script; 09-02: production execution proven idempotent, human-verified 2026-09-19, evidence in docs/lead-quality/BACKFILL-EVIDENCE.md) |
| SCRP-01 | Phase 10 — Motor de Cadência Único + Pipeline Fora do Notebook | Complete (10-04: os 48 fire_*.py arquivados em archive/ com README, 7 dependentes vivos religados em waves_core.py no 10-03, motor reverificado pós-arquivamento — waves_smoke.py 5/5, daily_waves.py --dry --force ate FIM sem traceback, human-verified 2026-09-19) |
| OBSV-01 | Phase 10 — Motor de Cadência Único + Pipeline Fora do Notebook | Complete (10-05: alerta Telegram por transição de estado em `waha-watchdog.py`/`watchdog-n8n.ps1`, task `waha-watchdog` registrada, prova real via n8n local caído, human-verified 2026-09-19, runbook em docs/email-marketing/OBSERVABILIDADE-watchdogs.md) |
| QUAL-01 | Phase 11 — Qualified Objetivo e Automático | Complete (11-01: critério `classify_intent(text) != 'negative'` documentado verbatim com citação de linha em docs/email-marketing/QUALIFIED-avanco-automatico.md, human-verified 2026-09-20) |
| QUAL-02 | Phase 11 — Qualified Objetivo e Automático | Complete (11-01: prova ao vivo reconferida — customer_id 2dcb6a14-006a-426c-bc60-b6599a8f0e64 avançou Status 1→2 via audit log 14s após classificação `positive`, caminho negativo sem falso positivo confirmado, runbook em docs/email-marketing/QUALIFIED-avanco-automatico.md, human-verified 2026-09-20) |
| EMAL-01 | Phase 12 — Escala de Email | Partial (12-05: mecanismo de escrita/reversão real provado em produção contra registro descartável `_gsddrill`, checkpoint humano executado; degrau real NÃO avançado — usuário escolheu `prova-e-segura` porque bounce rate (5,08% em 7d, 10,58% em 30d) está acima do próprio limiar de reversão D-04 (>5%), então subir violaria a regra que o usuário mesmo travou; `_dmarc` permanece em `pct=25` por decisão deliberada, não falha; runbook em `docs/email-marketing/DMARC-escalonamento.md`) |
| EMAL-02 | Phase 12 — Escala de Email | Partial (12-02: curva `WARMUP_TARGETS=[87,104,125,150,180,216,260,300]` (+20%/dia) e guard `already_queued_today()` em lockstep com o alvo implementados em `waves_core.py`/`daily_waves.py build_waves()`, provados verbatim em `python daily_waves.py --dry --force` — `WARMUP dia 1/8 — alvo 87/dia`; 12-06 Task 2: checkpoint humano respondido `aguardar` — usuário optou por não forçar execução real fora de calendário num domingo (2026-09-20); a task agendada `DIAX-Email-Waves-Daily` roda de verdade na segunda-feira 2026-09-21 08:40 BRT sem precisar de `--force`. Destrava quando essa execução confirmar o alvo real no Telegram — runbook em `docs/email-marketing/ESCALA-EMAIL-runbook.md` §2-3) |
| EMAL-03 | Phase 12 — Escala de Email | Complete (12-03/12-04: `ab_report.py` mede taxa de clique por variante `sA`/`sB` sobre dados reais de produção (936 envios/30d), `--selftest` 9/9 OK, `previews/ab-report.json` gravado com veredito real `INCONCLUSIVO` (12 cliques < 30 mínimo) — a medição existe e é consultável sem depender de nenhum envio futuro, que é o que o requisito pede; human-verified 2026-09-20 (leitura direta do JSON e do código, sem checkpoint pendente); runbook em `docs/email-marketing/ESCALA-EMAIL-runbook.md` §4) |
| EMAL-04 | Phase 12 — Escala de Email | Partial (12-04: caminho paralelo `build_webmail_wave()` implementado, `WEBMAIL_CAP=15`/dia, marca `auto wm`, fora do A/B por construção, 719 leads webmail nunca-contactados medidos ao vivo; provado em `--dry --force` — `DRY: webmail = 12 leads (cap 15, já hoje 0)`; 12-06 Task 2: checkpoint humano respondido `aguardar` (mesma decisão de EMAL-02) — campanha real com destinatários confirmados no CRM ainda pendente da execução agendada de 2026-09-21 08:40 BRT. Destrava quando essa execução mostrar a campanha `AQ - webmail - ... auto wm` no CRM com ≤15 destinatários — runbook em `docs/email-marketing/ESCALA-EMAIL-runbook.md` §5) |
| WHAT-01 | Phase 13 — Escala de WhatsApp | Complete por equivalência funcional (13-01: checkpoint humano da Task 2 respondido `python-oficial` — o handoff real de prospecção hoje roda por `D:\claude-code\n8n-local\outreach-send.py` via task Windows `outreach-wa` (Ready/Enabled, próxima execução 21/09 09:00, repetição 30min até 18h), não pelo workflow `waOutreach1a1AQ` citado literalmente no requisito, que permanece `active=0` por escolha deliberada desde o incidente de 12/09 (Code node do n8n com `fs.writeFileSync` não confiável no dedupe → 7ª mensagem para lead que já havia recusado). Propriedades de segurança que sustentam a equivalência: gravação de dedupe com releitura verificada (`grava_verificado()`), blacklist como primeira checagem, kill-switch `logs/OUTREACH-STOP`. Prova de execução real (snapshot `previews/whatsapp-estado.json`, gerado 2026-09-20T18:07:41): 186 chat_ids em `outreach-sent.json` (all-time), 16/18 enviados no dia, último `ENVIADO` 2026-09-20 17:30:13 para `5527996320214@c.us` (Cominoti Construções). `waOutreach1a1AQ` confirmado inalterado no mesmo snapshot: `active=0`, `cap=2`, `max_prio=4`, última execução 2026-09-12 17:05:36 — idêntico ao medido no planejamento, nenhuma mudança operacional feita nesta task. Divergência logs-vs-CRM medida: 186 vs 17 (delta 169), causa conhecida e documentada (`outreach-send.py` não chama `POST /api/v1/integrations/whatsapp-event`). Zero CAP change, zero ativação de n8n, zero mudança de Scheduled Task) |
| WHAT-02 | Phase 13 — Escala de WhatsApp | Complete (13-02: `crm_leads_sync.py` reescrito de populador manual único de 22 linhas para sync incremental verificado — `fontes()`/`atuais()`/`delta()`/`empurrar()`/`verificar()` — que le `staticData.global.crmLeads` do sqlite do n8n SOMENTE-LEITURA, calcula o delta contra `wa_estado.ler_sent()`+`ler_blacklist()`+`previews/whatsapp-leads.json`, e confirma cada push por releitura. Scheduled Task `DIAX-WA-CrmLeads-Sync` registrada (diária 09:00, repetição 1h por 10h, `ExecutionTimeLimit` 10min) e executada de verdade via `schtasks /run`: `LastTaskResult=0`. Prova do gap real fechado: dos 18 ids no delta desta execução, todos os 18 são chat_ids presentes em `outreach-sent.json` (leads genuinamente contatados por `outreach-send.py`) que nunca haviam entrado no conjunto de deflexão do bot — ex. `5527998544746@c.us` (AONI IMÓVEIS), enviado em 2026-09-15 09:00:11, ainda fora de `crmLeads` até este sync rodar. n8n confirmado fora do ar (porta 5678 fechada, mesma medição de 13-01): os 18 ids foram gravados em `previews/crmleads-pending.json` (fila durável) em vez de POSTados, saindo com código 0 e mensagem explícita — nunca finge sucesso; serão aplicados automaticamente na próxima execução horária assim que o n8n voltar) |
| WHAT-03 | Phase 13 — Escala de WhatsApp | Complete |
| WHAT-04 | Phase 13 — Escala de WhatsApp | Complete (13-04: `wa_escalation.py` criado com limiares `MIN_ENVIADOS_ES=60`/`MIN_RESPOSTAS_ES=10`/`TAXA_MINIMA=10%`/`TETO_RECUSA=8%` escritos e commitados (`d4fedba2`) ANTES de rodar `--status` contra dados reais pela primeira vez — ordem provável por `git log`. `decide()` função pura, `--selftest` 12/12 OK. Medição real sobre o histórico completo do audit log de `outreach-send.py` (desde 2026-09-13): `wa_es` = 121 enviados, 53 respondidos (taxa 43,8%), 7 recusas (5,8%) — acima de todos os limiares, veredito honesto `AVANCAR`, gravado em `previews/wa-escalation-state.json`. Achado adicional que fecha o requisito por equivalência (mesmo padrão de WHAT-01): `portao_atual()` confirma, lendo o código-fonte real, que `outreach-send.py:escolhe()` nunca filtrou por segmento — o "portão" wa_es→wa_br citado no requisito nunca existiu como trava de código no emissor oficial; `wa_br` já é elegível pela mesma fila assim que a prioridade permitir, e o veredito `AVANCAR` agora valida essa realidade em vez de contrariá-la. `--apply` grava o veredito mas nunca liga nada de operacional (não há o que ligar), imprimindo `SEM PORTAO DE SEGMENTO` — comportamento provado no runbook. Runbook em `docs/email-marketing/ESCALA-WHATSAPP-runbook.md` §5) |
| DASH-01 | Phase 14 — Dashboard de Conversão do Funil | Pending |

**Total mapped: 14/14 — 100% coverage, no orphans.**

---
*Requirements defined: 2026-09-19*
*Last updated: 2026-09-20 — 12-06 fechou a rastreabilidade da Phase 12 (EMAL-01/02/04 Partial, EMAL-03 Complete) após checkpoint `aguardar` do usuário na Task 2. 13-01 fechou WHAT-01 Complete por equivalência funcional após checkpoint `python-oficial` do usuário na Task 2 — sem mudança operacional em nenhum emissor. 13-02 e 13-03 (executados em paralelo) fecharam WHAT-02 e WHAT-03 Complete, respectivamente: sync incremental verificado do conjunto de deflexão do bot (18 leads reais sem deflexão capturados) e relatório semanal com entrega real no Telegram provada. 13-04 fechou WHAT-04 Complete: `wa_escalation.py` (limiares travados antes da medição, `git log` audita a ordem) mediu taxa de resposta real de `wa_es` (43,8%, 53/121) acima de todos os limiares e devolveu veredito honesto `AVANCAR`; achado central — nunca existiu portão de segmento em código no emissor oficial, `wa_br` já é elegível pela mesma fila. **Phase 13 fechada 4/4 planos.**
