# Phase 13: Escala de WhatsApp - Context

**Gathered:** 2026-09-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Handoff n8n→WAHA confirmado em execução. Bot IA desvia lead de prospecção pra humano em vez de
responder. Relatório semanal do canal. Escala wa_es→wa_br após validar taxa de resposta.

</domain>

<decisions>
### Relatório semanal (WHAT-03, único gray area discutido)

- **D-01:** Entrega via **Telegram** (mesmo canal já usado pelo watchdog/DMARC — consistência,
  reusar `alerta_telegram.py`/padrão de mensagem já estabelecido).
- **D-02:** Sai **sexta à tarde**, mesmo padrão de cadência do A/B de email (Fase 12) — fecha a
  semana.

### WHAT-01 — ativação real (fechado após research)

Research achou que `waOutreach1a1AQ` está **active=0** no n8n (não é fase de verificação como
esperado — precisa ativar de verdade) e ainda tem `CAP=1` (valor de teste, documentado deveria ser
20/dia).

- **D-03:** Sobe o CAP de 1 pra um valor conservador (ex. 5-10/dia, não direto pro 20 documentado) e
  ativa o workflow (`active=1`) só com **checkpoint humano antes** — mesmo padrão de cautela usado
  no warmup de volume/DMARC da Fase 12. Confirmar via execution log do n8n que rodou de verdade
  após ativar, não só que o toggle mudou.

### Claude's Discretion (não discutido a fundo — usar bom senso, documentar decisão tomada)
- **WHAT-02** (bot desvia pra humano): critério de "é prospecção" vs atendimento normal fica a
  critério de implementação — pesquisar como o bot atual (`8nKTlX4Y7Hy1EUEn` mencionado no
  REQUIREMENTS.md) diferencia contexto hoje antes de decidir a heurística.
- **WHAT-04** (escalar wa_es→wa_br): critério de "taxa de resposta validada" e ritmo de escala
  ficam a critério de implementação — pesquisar que dado de resposta já existe pro segmento `wa_es`
  antes de definir o limiar (por analogia ao que foi feito com bounce rate no DMARC, Fase 12).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### WhatsApp / n8n
- `D:\claude-code\n8n-local\workflows\local\wa-outreach-1a1.json` — o workflow de handoff (WHAT-01)
- `n8n-local/waha-watchdog.py`, `watchdog-n8n.ps1`, `alerta_telegram.py` (Phase 10) — padrão de
  alerta Telegram e de extensão de script existente, reusar estilo pro relatório semanal
- REQUIREMENTS.md — menciona bot `8nKTlX4Y7Hy1EUEn` (WHAT-02) e segmentos `wa_es` (480)/`wa_br`
  (1138) (WHAT-04) — localizar esses IDs/segmentos no n8n-local antes de planejar

### Runbooks de referência (formato a seguir)
- `docs/email-marketing/OBSERVABILIDADE-watchdogs.md` (Phase 10)
- `docs/email-marketing/QUALIFIED-avanco-automatico.md` (Phase 11)
- `docs/email-marketing/ESCALA-EMAIL-runbook.md` (Phase 12)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `alerta_telegram.py` — extensível pro relatório semanal (mesmo canal, mesmo helper)
- Padrão de escalonamento gradual com limiar de segurança + reversão (Phase 12, DMARC) — mesmo
  raciocínio aplicável ao WHAT-04 (validar antes de escalar)

### Established Patterns
- Fases de "verificar e documentar" quando a automação já existe (Phase 11) vs. "construir"
  quando não existe — pesquisar primeiro qual caso é cada um dos 4 requisitos desta fase antes de
  assumir que tudo precisa ser construído do zero

</code_context>

<specifics>
## Specific Ideas

- Relatório semanal no mesmo ritmo/canal que já foi decidido pro A/B de email — consistência
  entre os dois canais (email e WhatsApp)

</specifics>

<deferred>
## Deferred Ideas

- Nenhuma — discussão ficou dentro do escopo da fase

</deferred>

---

*Phase: 13-escala-de-whatsapp*
*Context gathered: 2026-09-20*
