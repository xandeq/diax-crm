# Phase 11: Qualified Objetivo e Automático - Context

**Gathered:** 2026-09-20
**Status:** Ready for planning

<domain>
## Phase Boundary

O avanço Contacted→Qualified tem critério objetivo documentado e já acontece automaticamente. Não
é construir automação nova — é verificar, provar e documentar a que já existe.

</domain>

<decisions>
### Escopo real (descoberto em discuss-phase — QUAL-01/QUAL-02 já implementados)

`docs/email-marketing/reply_watch.py` (commitado, não é o diff sujo — o diff sujo é só texto de
saudação, não toca a lógica abaixo) já implementa exatamente o que QUAL-01/QUAL-02 pedem:

- `classify_intent(text)` classifica a resposta do lead em `negative`/`positive`/`neutral` via
  regex (`NEG`/`POS`)
- `main()` linha 280-283: se `intent == 'negative'` → `suppress(em)` (opt-out); senão, se
  `lead.get('customer_id')` existe e `status < 2` → `crm_status(H, lead['customer_id'], 2)`
  (avança pra Qualified)
- Roda via task agendada `DIAX-Reply-Watch` (confirmada existente na pesquisa da Phase 10)
- Cria tarefa no CRM + alerta Telegram com rascunho de resposta a cada reply tratado

- **D-01 (decisão do usuário):** Escopo da Phase 11 é **só verificar e documentar**, não estender
  nem enrijecer o critério agora. WhatsApp não ter a mesma automação (só cria tarefa, não avança
  status) fica como gap conhecido, não corrigido nesta fase. Critério "não-negativo = Qualified"
  fica como está, não fica mais rigoroso agora.
- **D-02:** A fase precisa provar — com evidência real, mesmo padrão de `BACKFILL-EVIDENCE.md`
  (Phase 9) e `OBSERVABILIDADE-watchdogs.md` (Phase 10) — que `DIAX-Reply-Watch` de fato roda
  (Scheduled Task `Ready`/`Running`, não `Disabled`) e que o `crm_status` funcionou pelo menos uma
  vez de verdade (rastrear um `customer_id` real que foi de status<2 pra 2 via este mecanismo).
- **D-03:** Documentar QUAL-01 como requisito formal — o critério objetivo é literalmente
  `classify_intent(text) != 'negative'` — em algum lugar rastreável (runbook novo ou seção em
  `docs/email-marketing/OBSERVABILIDADE-watchdogs.md`), para não ficar só implícito no código.

### Claude's Discretion
- Onde exatamente documentar (novo arquivo vs. seção em runbook existente)
- Como provar o "pelo menos uma vez" do D-02 (query no banco/API vs. teste controlado)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Automação existente (QUAL-01/QUAL-02)
- `docs/email-marketing/reply_watch.py` — ler por completo; função `classify_intent` (critério),
  `main()` linhas 242-288 (fluxo completo), `crm_status`/`crm_task`/`tg` (efeitos colaterais)
- `docs/email-marketing/OBSERVABILIDADE-watchdogs.md` (Phase 10) — padrão de runbook a seguir pro
  novo doc desta fase

### Verificação
- Scheduled Task `DIAX-Reply-Watch` — status real via `Get-ScheduledTask`
- `docs/email-marketing/previews/reply-state.json` (ou equivalente — nome exato a confirmar em
  `load_state()`/`save_state()` de `reply_watch.py`) — histórico de `seen`/`last_uid`, prova de
  execuções passadas

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `reply_watch.py` já é a implementação completa — nada novo a construir, só verificar

### Established Patterns
- Mesmo padrão de evidência operacional (sem test framework) usado nas Phases 9 e 10 — documento
  markdown com prova antes/depois, não teste automatizado

### Integration Points
- `crm_status` chama `PATCH /api/v1/customers/{id}/status` — mesmo endpoint que
  `mcp__diax-crm__update_lead_status` usa

</code_context>

<specifics>
## Specific Ideas

- "Só verificar/documentar" — não quer risco de mexer numa automação que já funciona sozinha

</specifics>

<deferred>
## Deferred Ideas

- Estender auto-avanço pra Qualified também no canal WhatsApp — fica pra depois, não é esta fase
- Enrijecer o critério "não-negativo = Qualified" (hoje aceita resposta neutra tipo "quem é você?")
  — fica pra depois, requer decisão do usuário sobre o que conta como interesse real

</deferred>

---

*Phase: 11-qualified-objetivo-e-autom-tico*
*Context gathered: 2026-09-20*
