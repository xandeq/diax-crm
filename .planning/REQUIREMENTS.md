# Requirements: DIAX CRM — v1.3 Pipeline de Aquisição

**Defined:** 2026-09-05
**Core Value:** Centralizar todas as operações de negócio em um único sistema pessoal, eliminando ferramentas externas pagas

## v1 Requirements

Requirements para o milestone v1.3. Cada um mapeia pra uma fase do roadmap.

### Extração

- [x] **EXTR-01**: Sistema rejeita lead cujo domínio de email não tem MX/A válido antes de importar (worker .NET `ExtractorIntegrationService` — hoje só a ponte Python manual faz essa checagem)
- [ ] **EXTR-02**: Sistema registra o motivo de rejeição (geo fora do alvo / email lixo / sem MX / duplicado) de cada lead descartado do Extrator, consultável por período
- [x] **EXTR-03**: Sistema classifica o `website` do lead como "site próprio" vs "diretório de terceiro" (econodata, cliniguia, redes sociais) e usa isso como sinal no import/score

### Import CRM

- [ ] **IMPT-01**: Sistema deduplica leads do Extrator por ID externo (`Customer.ExternalId`, com índice único), não só por email
- [ ] **IMPT-02**: `POST /api/v1/customers/import` deduplica de verdade para `source=Scraping` (hoje só funciona para `source=Import`)
- [ ] **IMPT-03**: Sistema calcula `lead_score` no momento do import, sem esperar o job diário do `LeadScoringWorker` (06h BRT)

## v2 Requirements

Deferidos para milestone futuro. Escopo confirmado com o usuário em 2026-09-05.

### Email

- **EMAL-01**: DMARC sobe de `pct=25` para `pct=100`
- **EMAL-02**: Volume de envio escala de 87 para 300/dia com warmup gradual
- **EMAL-03**: A/B de subject (variant A/B) tem o resultado medido e reportado (hoje só alterna, ninguém lê)
- **EMAL-04**: Leads "webmail" (gmail/hotmail comercial, 560 hoje ignorados) testados com cap pequeno + tag própria

### WhatsApp

- **WHAT-01**: Confirmar execução do handoff (`n8n-wa-outreach-1a1.json`) pela sessão que opera o WAHA local
- **WHAT-02**: Bot inbound de IA (`8nKTlX4Y7Hy1EUEn`) desvia lead de prospecção pra humano em vez de responder
- **WHAT-03**: Relatório semanal de métricas do canal WhatsApp (enviados/respondidos/opt-out)
- **WHAT-04**: Escalar de `wa_es` (480 leads) para `wa_br` (1138) após validar taxa de resposta em ES

### Extração (investigativo)

- **EXTR-04**: Avaliar viabilidade do Google Places API como fonte alternativa/complementar de leads (endereço/telefone verificados vs. scraping de página)

### Observabilidade

- **OBSV-01**: Reduzir o ponto único de falha do pipeline (waves, vigia de respostas, n8n, WAHA rodam só no notebook via Task Scheduler)

## Out of Scope

Explicitamente excluído deste milestone. Documentado para não voltar sem contexto.

| Feature | Reason |
|---------|--------|
| Prova social real no `/portfolio` | Decisão de conteúdo do usuário, não bloqueia entrega técnica de v1.3 |
| Agentes de IA (v1.2) | Milestone separado, pausado por sessão concorrente — não mexer nos arquivos de `src/Diax.Domain/Agents/*` durante v1.3 |

## Traceability

Preenchido pelo roadmapper.

| Requirement | Phase | Status |
|-------------|-------|--------|
| EXTR-01 | Phase 7 | Complete |
| EXTR-02 | Phase 7 | Pending |
| EXTR-03 | Phase 7 | Complete |
| IMPT-01 | Phase 8 | Pending |
| IMPT-02 | Phase 8 | Pending |
| IMPT-03 | Phase 8 | Pending |

**Coverage:**
- v1 requirements: 6 total
- Mapped to phases: 6
- Unmapped: 0 ✓

---
*Requirements defined: 2026-09-05*
*Last updated: 2026-09-05 — roadmap created (Phase 7: Extração, Phase 8: Import CRM), 6/6 requirements mapped*
