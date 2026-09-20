# Phase 12: Escala de Email - Context

**Gathered:** 2026-09-20
**Status:** Ready for planning

<domain>
## Phase Boundary

DMARC sobe de pct=25 para pct=100 de forma escalonada e monitorada. Volume de envio escala de 87
para 300/dia com warmup. A/B de subject tem resultado medido. Leads "webmail" (560) testados com
cap e tag próprios.

</domain>

<decisions>
### DMARC (D-01, D-02)

Registro atual confirmado via DNS ao vivo: `_dmarc.alexandrequeiroz.com.br` =
`v=DMARC1; p=quarantine; pct=25; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r`.

**Domínio está no HostGator/WHM (conta `alexa084`), NÃO no Cloudflare** — confirmado ao vivo
(`GET /zones?name=alexandrequeiroz.com.br` no Cloudflare retornou 0 resultados). Qualquer automação
de DNS usa a **API do WHM** (`WHM_HOST`/`WHM_USER`/`WHM_API_TOKEN` em `.secrets.env`, ver
`~/.claude/docs/reference-infra.md`), não a API Cloudflare que os scripts anteriores desta sessão
usaram para outros domínios.

- **D-01:** Escalona 25%→50%→75%→100%, **1 degrau por semana** (4 semanas total). Antes de subir
  cada degrau, checar relatório de bounce/spam-complaint do Brevo (a fonte já é `rua@dmarc.brevo.com`
  — usar o relatório agregado do Brevo, não reinventar parsing de DMARC aggregate report).
- **D-02:** Se um degrau disparar bounce/complaint alto, **reverte automaticamente** pro degrau
  anterior via WHM API, e avisa via Telegram (mesmo canal/padrão do watchdog da Phase 10 —
  `alerta_telegram.py` ou variante). "Alto" — definir limiar em research/planning com base no que
  o relatório Brevo já expõe (ex. bounce rate > X% ou spam complaint > Y no período do degrau).

### Volume (D-03)

- **D-03:** Escalona 87→300/dia em **+20%/dia** (87→104→125→150→180→216→260→300, ~8 dias úteis).
  Ritmo padrão de warmup de deliverability — não é negociável pra mais rápido.

### Claude's Discretion (áreas não discutidas — usar bom senso, documentar decisão tomada)

- **A/B de subject:** qual métrica decide vencedor (open rate vs. reply rate) e onde o resultado
  fica visível — não discutido a fundo, pesquisar o que o Brevo já expõe via API antes de decidir.
- **Leads webmail (560):** cap diário e nome da tag — não discutido a fundo, usar critério
  conservador (ex. cap pequeno tipo 10-20/dia) dado que domínio webmail tende a ter reputação pior.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### DMARC / DNS
- `~/.claude/docs/reference-infra.md` — seção HostGator/WHM (credenciais, API de zona DNS) e
  Registro.br (dono do domínio, mas DNS aponta pro WHM)
- Registro DMARC atual (capturado ao vivo nesta sessão): ver texto acima, não re-adivinhar

### Volume / A-B / webmail
- `docs/email-marketing/daily_waves.py` (Phase 10) — motor real, onde o volume/segmentação é
  decidido hoje (`MAX_PER_SEG`, `HEALTHY` providers)
- `docs/email-marketing/waves_core.py` (Phase 10) — `HEALTHY`, `PER_PROVIDER`, `assign_providers` —
  provavelmente onde o warmup de volume se conecta
- Runbook de referência de formato: `docs/email-marketing/OBSERVABILIDADE-watchdogs.md` (Phase 10),
  `docs/email-marketing/QUALIFIED-avanco-automatico.md` (Phase 11)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `alerta_telegram.py` (Phase 10, `D:/claude-code/n8n-local/`) — padrão de alerta transição-de-estado,
  reusar mesmo estilo pro alerta de DMARC/volume
- `waves_core.py`/`daily_waves.py` — MAX_PER_SEG e HEALTHY já controlam volume por segmento/provider,
  ponto de entrada natural pro warmup escalonado

### Established Patterns
- Auth CRM: `DIAX_ADMIN_EMAIL`/`DIAX_ADMIN_PASSWORD` via `fetch_and_classify_leads.load_secrets/login`
- Estado persistido em JSON (`waves-baseline.json`, `logs/watchdog-estado.json`) — mesmo padrão
  provavelmente serve pra rastrear qual degrau de DMARC/volume está ativo hoje

### Integration Points
- WHM API (nova pra este projeto — Phases 9/10/11 não tocaram DNS) — `~/.claude/docs/reference-infra.md`
  tem exemplos de chamada, mas nenhum exemplo específico de edição de registro TXT existente
  (`editzonerecord`) — pesquisar a chamada exata antes de planejar

</code_context>

<specifics>
## Specific Ideas

- Escalonamento de 1 semana por degrau no DMARC — dar tempo real de observar impacto antes de
  avançar, não é pressa
- Reversão automática, não manual — se o script detecta problema, ele mesmo volta o degrau

</specifics>

<deferred>
## Deferred Ideas

- Nenhuma — discussão ficou dentro do escopo da fase

</deferred>

---

*Phase: 12-escala-de-email*
*Context gathered: 2026-09-20*
