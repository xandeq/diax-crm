# Phase 14: Dashboard de Conversão do Funil - Context

**Gathered:** 2026-09-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Dashboard de conversão percentual por etapa do funil (Lead→Contacted→Qualified→Customer) existe e
está correto. Fase de verificação/documentação — não construção nova.

</domain>

<decisions>
### Escopo real (descoberto em discuss-phase — DASH-01 já implementado)

`crm-web/src/app/dashboard-comercial/page.tsx` já renderiza o funil com barras proporcionais e
`conversionToNext` (percentual entre cada etapa). Backend: `Diax.Application/Customers/
SalesDashboardService.cs` já calcula `ConversionToNext`. Isso é exatamente o que DASH-01 pede.

- **D-01 (decisão do usuário):** Escopo é **só verificar e documentar** — mesmo padrão da Phase 11
  (QUAL-01/02). NÃO soma as métricas de email/WhatsApp (Fases 12/13) nessa tela agora — fica
  deferred.
- **D-02:** Verificar com dado real: os percentuais de `conversionToNext` batem com os números reais
  do funil (comparar contra `dashboard_summary` / funnel counts já vistos nesta sessão: Lead 4.201,
  Contacted 2.951, Qualified 31, etc. pós Phase 9). Confirmar que a página carrega de verdade
  (screenshot ou captura de tela, dado que é frontend) — não só que o endpoint responde.

### Claude's Discretion
- Se achar algum gap real (ex. página não reflete os dados pós-Phase 9 corretamente), decidir se é
  bug a corrigir nesta fase ou achado a documentar/deferir

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Dashboard existente (DASH-01)
- `crm-web/src/app/dashboard-comercial/page.tsx` — frontend, ler por completo
- `api-core/src/Diax.Application/Customers/SalesDashboardService.cs` — backend, `ConversionToNext`
- `api-core/src/Diax.Api/Controllers/V1/SalesDashboardController.cs` — endpoint

### Runbooks de referência (formato a seguir)
- `docs/email-marketing/QUALIFIED-avanco-automatico.md` (Phase 11) — mesmo padrão "já existia, só
  precisou provar"
- `docs/email-marketing/ESCALA-WHATSAPP-runbook.md` (Phase 13)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Dashboard completo já funcional — nada novo a construir

### Established Patterns
- Padrão desta milestone: fases de "verificar e documentar automação/feature já existente" (9, 11,
  parte da 13) vs. "construir" (10, 12) — esta é claramente do primeiro tipo

</code_context>

<specifics>
## Specific Ideas

- Nenhuma — só confirmar que o que já existe está correto

</specifics>

<deferred>
## Deferred Ideas

- Somar métricas de email/WhatsApp (Fases 12/13) na mesma tela do dashboard comercial — fica pra
  milestone futura, não é esta fase

</deferred>

---

*Phase: 14-dashboard-de-convers-o-do-funil*
*Context gathered: 2026-09-20*
