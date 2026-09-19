# Requirements: DIAX CRM — Milestone v1.4

**Defined:** 2026-09-19
**Core Value:** Centralizar todas as operações de negócio (leads, finanças, comunicação, IA) em um único sistema pessoal, eliminando ferramentas externas pagas

## v1 Requirements

Requirements para o milestone v1.4 "Funil de Outreach — Cadência e Qualidade". Cada um mapeia para uma fase do roadmap.

### Backfill

- [ ] **BFIL-01**: Backlog pré-v1.3 (~5.266 leads que nunca passaram pelo `LeadScoringService`) tem `LeadScore`/`Segment` recalculados usando o motor oficial (C#), não o script Python ad-hoc criado em sessão de auditoria manual (2026-09-17/19)

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

- [ ] **OBSV-01**: Pipeline de outreach (waves, vigia de respostas, n8n, WAHA) deixa de depender só do notebook do usuário via Task Scheduler — ponto único de falha
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

Preenchido pelo roadmapper.

---
*Requirements defined: 2026-09-19*
*Last updated: 2026-09-19 after initial definition*
