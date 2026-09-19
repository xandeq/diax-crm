# Requirements: DIAX CRM — Milestone v1.4

**Defined:** 2026-09-19
**Core Value:** Centralizar todas as operações de negócio (leads, finanças, comunicação, IA) em um único sistema pessoal, eliminando ferramentas externas pagas

## v1 Requirements

Requirements para o milestone v1.4 "Funil de Outreach — Cadência e Qualidade". Cada um mapeia para uma fase do roadmap.

### Backfill

- [x] **BFIL-01**: Backlog pré-v1.3 (~5.266 leads que nunca passaram pelo `LeadScoringService`) tem `LeadScore`/`Segment` recalculados usando o motor oficial (C#), não o script Python ad-hoc criado em sessão de auditoria manual (2026-09-17/19)

### Email

- [ ] **EMAL-01**: DMARC sobe de `pct=25` para `pct=100`
- [ ] **EMAL-02**: Volume de envio escala de 87 para 300/dia com warmup gradual
- [ ] **EMAL-03**: A/B de subject (variant A/B) tem o resultado medido e reportado (hoje só alterna, ninguém lê)
- [ ] **EMAL-04**: Leads "webmail" (gmail/hotmail comercial, 560 hoje ignorados) testados com cap pequeno + tag própria

### WhatsApp

- [ ] **WHAT-01**: Handoff (`n8n-wa-outreach-1a1.json`) confirmado em execução pela sessão que opera o WAHA local
- [ ] **WHAT-02**: Bot inbound de IA (`8nKTlX4Y7Hy1EUEn`) desvia lead de prospecção pra humano em vez de responder
- [ ] **WHAT-03**: Relatório semanal de métricas do canal WhatsApp (enviados/respondidos/opt-out)
- [ ] **WHAT-04**: Escalar de `wa_es` (480 leads) para `wa_br` (1138) após validar taxa de resposta em ES

### Funil / Qualified

- [ ] **QUAL-01**: Critério objetivo de avanço Contacted→Qualified definido e documentado (hoje é implícito/manual)
- [ ] **QUAL-02**: `reply_watch.py` avança status automaticamente no CRM quando o critério QUAL-01 é atingido

### Consolidação

- [ ] **SCRP-01**: Motor único parametrizado (nicho/wave/data como argumentos) substitui os 79+ scripts `fire_*.py` soltos em `docs/email-marketing/`

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
| SCRP-01 | Phase 10 — Motor de Cadência Único + Pipeline Fora do Notebook | Pending (10-01/02/03 done — só falta 10-04 arquivar fire_*.py) |
| OBSV-01 | Phase 10 — Motor de Cadência Único + Pipeline Fora do Notebook | Complete (10-05: alerta Telegram por transição de estado em `waha-watchdog.py`/`watchdog-n8n.ps1`, task `waha-watchdog` registrada, prova real via n8n local caído, human-verified 2026-09-19, runbook em docs/email-marketing/OBSERVABILIDADE-watchdogs.md) |
| QUAL-01 | Phase 11 — Qualified Objetivo e Automático | Pending |
| QUAL-02 | Phase 11 — Qualified Objetivo e Automático | Pending |
| EMAL-01 | Phase 12 — Escala de Email | Pending |
| EMAL-02 | Phase 12 — Escala de Email | Pending |
| EMAL-03 | Phase 12 — Escala de Email | Pending |
| EMAL-04 | Phase 12 — Escala de Email | Pending |
| WHAT-01 | Phase 13 — Escala de WhatsApp | Pending |
| WHAT-02 | Phase 13 — Escala de WhatsApp | Pending |
| WHAT-03 | Phase 13 — Escala de WhatsApp | Pending |
| WHAT-04 | Phase 13 — Escala de WhatsApp | Pending |
| DASH-01 | Phase 14 — Dashboard de Conversão do Funil | Pending |

**Total mapped: 14/14 — 100% coverage, no orphans.**

---
*Requirements defined: 2026-09-19*
*Last updated: 2026-09-19 — roadmap criado (Phases 9-14), traceability preenchida pelo roadmapper.*
