# Phase 10: Motor de Cadência Único + Pipeline Fora do Notebook - Context

**Gathered:** 2026-09-19
**Status:** Ready for planning

<domain>
## Phase Boundary

O disparo de outreach roda por um motor único parametrizado — não mais 79+ scripts `fire_*.py`
soltos — e existe visibilidade quando uma etapa crítica do pipeline (WAHA/n8n) cai, em vez de
falhar em silêncio. Migração de infraestrutura (rodar em outra máquina) está fora deste phase.

</domain>

<decisions>
### Escopo real do SCRP-01 (revisado após descoberta em discuss-phase)

`daily_waves.py` **já é** o motor real — roda via Task Scheduler seg-sex 08:40 BRT, com follow-up
multi-toque (FUP1/2/3), circuit breaker, filtro de lead estrangeiro/role-mailbox, digest de
hot-leads no Telegram. Isso NÃO é reescrito do zero.

- **D-01:** `daily_waves.py` continua sendo o motor. O trabalho é extrair as funções/constantes que
  ele hoje importa de `fire_today_20.py` (`DIR`, `base`, `HEALTHY`, `PER_PROVIDER`, `ts`,
  `load_env`, `login`, `gen_image`, `fetch_leads`, `assign_providers`, `ensure_breaker_closed`),
  `fire_w1_2707.py` (`build_html_v2`, `SEGMENTS`) e `fire_w2w3_2707.py` (`WAVES`) para dentro de
  `waves_lib.py` (ou um módulo novo, ex. `waves_core.py`, se a mistura de responsabilidades pedir).
- **D-02:** Depois da extração, `fire_today_20.py`, `fire_w1_2707.py` e `fire_w2w3_2707.py` (e os
  outros ~76 scripts `fire_*.py`/`fire_w*.py` sem nenhum import ativo de `daily_waves.py`) são
  movidos para `docs/email-marketing/archive/` — não deletados, ficam acessíveis pra consulta de
  histórico de campanha antiga.
- **D-03:** Antes de mover qualquer arquivo, confirmar por `grep -rn "from fire_\|import fire_"` em
  todo `docs/email-marketing/` que nenhum outro script além de `daily_waves.py` depende deles — a
  extração precisa cobrir 100% dos imports reais antes do arquivamento, não só os 3 já identificados
  nesta sessão.

### Escopo real do OBSV-01 (revisado — sem migração de infra)

- **D-04:** Não migra nada de máquina. WAHA, n8n e o Task Scheduler continuam rodando no notebook do
  usuário. O que este phase entrega é **alerta**, não eliminação do ponto único de falha.
- **D-05:** Alerta dispara especificamente quando **WAHA ou n8n estão fora do ar** — não quando o
  Task Scheduler atrasa (isso já tem tolerância própria em `waves_lib.compute_wave_slots`) nem
  quando `daily_waves.py` roda mas envia 0 emails (fora de escopo desta fase, considerado
  aceitável/já coberto por outro sinal). Canal: o bot Telegram que já existe
  (`TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` em `~/.claude/.secrets.env`, já usado por
  `opportunity_detector` e pelo skill `dealhunter`) — reusar, não criar bot novo.
- **D-06:** Verificação de saúde: WAHA via `GET http://127.0.0.1:3000/api/health` (ou endpoint
  equivalente do WAHA — confirmar na pesquisa) checando o container `waha` já mapeado em
  `waha-hub/docker-compose.yml`; n8n via `N8N_WEB_URL`/API do n8n já em `.secrets.env`. Rodar como
  job periódico (mesma família de agendamento que já existe — Task Scheduler local, dado que D-04
  não migra infra).

### Claude's Discretion
- Nome exato do módulo novo (se `waves_lib.py` crescer demais, criar um segundo módulo é aceitável)
- Frequência exata do health-check (ex. a cada 15-30min é razoável; não foi discutido em detalhe)
- Formato exato da mensagem de alerta no Telegram

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Motor de cadência (SCRP-01)
- `docs/email-marketing/daily_waves.py` — o orquestrador real, ler por completo antes de planejar (imports no topo do arquivo mapeiam exatamente o que precisa ser extraído)
- `docs/email-marketing/waves_lib.py` — módulo de destino já existente, funções puras (compute_wave_slots, fup_candidates, is_foreign_lead, etc.) — extrair pra cá quando fizer sentido
- `docs/email-marketing/fire_today_20.py` — fonte de `load_env`/`login`/`fetch_leads`/`assign_providers`/`ensure_breaker_closed`/`HEALTHY`/`PER_PROVIDER` (a extrair)
- `docs/email-marketing/fire_w1_2707.py` — fonte de `build_html_v2`/`SEGMENTS` (a extrair)
- `docs/email-marketing/fire_w2w3_2707.py` — fonte de `WAVES` (a extrair)
- `docs/email-marketing/DIAX-Email-Waves-Daily.xml` — export do Task Scheduler, referência de como a task roda hoje

### Alerta de infra (OBSV-01)
- `waha-hub/docker-compose.yml` — container `waha`, porta 3000 local
- `~/.claude/.secrets.env` — `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID`, `N8N_WEB_URL`, `N8N_API_KEY` já existentes
- `D:\claude-code\opportunity_detector` (se existir um script de referência) e `skills/dealhunter/` — padrão já usado de notificação via Telegram, seguir o mesmo estilo

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `waves_lib.py` — já tem funções puras testáveis (compute_wave_slots, fup_candidates, is_foreign_lead, is_role_mailbox, personalize_html) — extensão natural pro que sai dos scripts fire_*
- Bot Telegram já configurado e em uso (opportunity_detector, dealhunter) — reusar credenciais e padrão de mensagem

### Established Patterns
- Scripts de email marketing carregam secrets lendo `~/.claude/.secrets.env` linha a linha e populando `os.environ` (visto em `fire_wave_today.py` e replicado em `daily_waves.py` via `fire_today_20.load_env`)
- Login no DIAX CRM via `POST /api/v1/auth/login` com `DIAX_ADMIN_EMAIL`/`DIAX_ADMIN_PASSWORD`, mesmo padrão usado nos scripts desta sessão (`fetch_and_classify_leads.py`, `backfill_lead_scores.py`)

### Integration Points
- `daily_waves.py` roda via Task Scheduler (`DIAX-Email-Waves-Daily.xml`) — qualquer refactor não pode quebrar essa invocação (`python daily_waves.py [--dry]`)
- Health-check de WAHA/n8n é um script/job NOVO e independente — não precisa se integrar com `daily_waves.py`, só compartilhar o canal de alerta (Telegram)

</code_context>

<specifics>
## Specific Ideas

- "Só alerta quando falhar" — não quer over-engineering de migração de infra agora, quer visibilidade primeiro
- Preocupação específica é WAHA/n8n caírem sem avisar, não o Task Scheduler atrasar (isso já é tolerado)

</specifics>

<deferred>
## Deferred Ideas

- Migrar waves/vigia/n8n/WAHA pra rodar numa VPS (eliminar o ponto único de falha de vez) — considerar se o alerta mostrar que cai com frequência
- Alertar também em "rodou mas 0 emails enviados" (circuit breaker aberto) — fora de escopo desta fase por decisão explícita do usuário

</deferred>

---

*Phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook*
*Context gathered: 2026-09-19*
