---
phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook
plan: 02
subsystem: email-marketing
tags: [python, extraction, refactor, production-critical, email-outreach]

# Dependency graph
requires: ["10-01"]
provides:
  - "waves_core.py — infra do motor: DIR/base/HEALTHY/PER_PROVIDER/FREE_WEBMAIL/FOREIGN_TLD + ts/load_env/login/qp/ensure_breaker_closed/build_html/gen_image/email_ok/_mx_deliverable/fetch_leads/assign_providers, extraido verbatim de fire_today_20.py"
  - "waves_copy.py — template de producao build_html_v2 + bancos de copy SEGMENTS (4 nichos)/WAVES (8 nichos), extraido verbatim de fire_w1_2707.py/fire_w2w3_2707.py"
  - "daily_waves.py religado: import waves_core/waves_copy no lugar de fire_today_20/fire_w1_2707/fire_w2w3_2707"
affects: [10-03, 10-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Extracao verbatim (copiar, nao mover) com prova de fidelidade via inspect.getsource comparison (7/7 identicos) e sha256 hash-freeze contra baseline congelado"
    - "HEALTHY/PER_PROVIDER/assign_providers mantidos no MESMO modulo (waves_core) para preservar o contrato de mutacao in-place `ft.HEALTHY[:] = healthy` que refresh_healthy() depende"

key-files:
  created:
    - docs/email-marketing/waves_core.py
    - docs/email-marketing/waves_copy.py
  modified:
    - docs/email-marketing/daily_waves.py

key-decisions:
  - "Dois modulos e nao um: waves_core.py (infra com I/O de rede: login/fetch_leads/gen_image) e waves_copy.py (template + banco de copy, importa DIR/qp de waves_core) — waves_lib.py continua intocado, reservado a funcoes puras testaveis offline"
  - "fire_today_20.py/fire_w1_2707.py/fire_w2w3_2707.py permanecem no disco, byte-a-byte inalterados (md5 conferido antes/depois) — arquivamento e responsabilidade do plano 10-04, apos os 7 dependentes vivos serem religados no 10-03"

requirements-completed: [SCRP-01]

# Metrics
duration: ~35min
completed: 2026-09-19
---

# Phase 10 Plan 02: Extração do Motor de Cadência para waves_core/waves_copy Summary

**Extração verbatim (copiar, não mover) de 17 símbolos de `fire_today_20.py`/`fire_w1_2707.py`/`fire_w2w3_2707.py` para dois módulos novos (`waves_core.py`, `waves_copy.py`), com `daily_waves.py` religado neles e prova de equivalência total (hash-freeze + inspect.getsource + dry-run com rede real).**

## Performance

- **Duration:** ~35 min
- **Completed:** 2026-09-19T22:15:00Z
- **Tasks:** 3
- **Files modified:** 3 (2 novos + 1 rewire)

## Accomplishments

- `waves_core.py` criado (241 linhas): 17 símbolos de infra (`DIR`, `base`, `HEALTHY`, `PER_PROVIDER`,
  `FREE_WEBMAIL`, `FOREIGN_TLD`, `ts`, `load_env`, `login`, `qp`, `ensure_breaker_closed`,
  `build_html`, `gen_image`, `email_ok`, `_mx_deliverable`, `fetch_leads`, `assign_providers`) —
  todos existem, `HEALTHY`/`PER_PROVIDER` batem com o esperado
- `waves_copy.py` criado (178 linhas): `build_html_v2` (template de produção, sha256 idêntico ao
  baseline), `SEGMENTS` (4 nichos: logistica/arquitetura/estudio/medico), `WAVES` (8 nichos:
  engenharia/advocacia/escola/marketing/moveis/academia/otica/pousada)
- `daily_waves.py` religado: bloco de import (linhas 21-23) trocado de `fire_today_20`/`fire_w1_2707`/
  `fire_w2w3_2707` para `waves_core`/`waves_copy` (aliases `SEG_W1`/`W23` preservados), e
  `refresh_healthy()` trocado de `import fire_today_20 as ft` para `import waves_core as ft` (alias
  `ft` preservado)
- `git diff docs/email-marketing/daily_waves.py` mostra exatamente as 2 edições pretendidas (4
  linhas do bloco de import + 1 linha do `refresh_healthy`), zero outras linhas tocadas
- `grep -n "fire_today_20\|fire_w1_2707\|fire_w2w3_2707" daily_waves.py` retorna zero linhas
- `python daily_waves.py --dry --force` rodou até `===== FIM =====` com rede real (login, fetch_leads,
  site_check), sem traceback, plano montado: `DRY: plano = [('marketing', 4), ('consultoria', 4)]`,
  zero `ImportError`/`NameError`/`KeyError` no log, zero campanha criada/enfileirada fora do prefixo
  `DRY`

## Fidelidade da Extração (evidência)

**md5 dos 3 arquivos fonte — antes e depois, inalterados:**
| Arquivo | md5 |
|---|---|
| `fire_today_20.py` | `a32493625f498ae25452862b59a2a7be` (idêntico antes/depois) |
| `fire_w1_2707.py` | `2e9f1e68429541a6d7ba95e8d66b055d` (idêntico antes/depois) |
| `fire_w2w3_2707.py` | `e777ee6fed7c02eeee3266028eec5d18` (idêntico antes/depois) |

**Comparação `inspect.getsource` — waves_core vs fire_today_20 (7/7 idênticos):**
```
fetch_leads True
email_ok True
assign_providers True
ensure_breaker_closed True
gen_image True
login True
load_env True
7 / 7
```

**Hash do template — waves_copy.build_html_v2(**ARGS_TEMPLATE) vs baseline:**
`05d50fa3620c8662ee3f1fbcf344f95ce5caf863e24f2933727caafb80f3fd49` == `template_v2_sha256` do
`waves-baseline.json` (match exato).

**waves_smoke.py — 5/5 OK após CADA task** (Task 1, Task 2 e Task 3), sem regredir em nenhum ponto:
```
OK rotation_coberta
OK copy_bank_integro
OK template_v2_identico
OK healthy_mutavel_in_place
OK imports_locais_resolvem
== 5/5 OK ==
```

**`daily_waves` religado — módulos corretos:**
```
daily_waves.assign_providers.__module__ == 'waves_core'
daily_waves.build_html_v2.__module__ == 'waves_copy'
len(daily_waves.COPY_BANK) == 20
set(daily_waves.ROTATION) - set(daily_waves.COPY_BANK) == set()  (vazio)
```

**Diff exato de `daily_waves.py` (única mudança de código do plano):**
```diff
-from fire_today_20 import (DIR, base, HEALTHY, PER_PROVIDER, ts, load_env, login,
-                           gen_image, fetch_leads, assign_providers, ensure_breaker_closed)
-from fire_w1_2707 import build_html_v2, SEGMENTS as SEG_W1
-from fire_w2w3_2707 import WAVES as W23
+from waves_core import (DIR, base, HEALTHY, PER_PROVIDER, ts, load_env, login,
+                        gen_image, fetch_leads, assign_providers, ensure_breaker_closed)
+from waves_copy import build_html_v2, SEGMENTS as SEG_W1, WAVES as W23
 import db as crmdb
 from waves_lib import (compute_wave_slots, brt_label, fup_candidates, is_role_mailbox, is_foreign_lead,
                        personalize_html, ACHADO_MARK)
@@ refresh_healthy() @@
-    import fire_today_20 as ft
+    import waves_core as ft
```

## Task Commits

Each task was committed atomically:

1. **Task 1: Criar waves_core.py com a infra extraída verbatim de fire_today_20.py** - `37d19c53` (feat)
2. **Task 2: Criar waves_copy.py com build_html_v2 + SEGMENTS + WAVES** - `28b83aa8` (feat)
3. **Task 3: Religar daily_waves.py nos módulos novos e provar equivalência total** - `bed2bc73` (feat)

## Files Created/Modified

- `docs/email-marketing/waves_core.py` - infra do motor (241 linhas): auth, fetch de leads, geração
  de imagem, circuit breaker; `HEALTHY`/`PER_PROVIDER`/`assign_providers` no mesmo módulo por
  restrição do research (mutação in-place)
- `docs/email-marketing/waves_copy.py` - template v2 + banco de copy (178 linhas): `build_html_v2`,
  `SEGMENTS`, `WAVES`; importa `DIR`/`qp` de `waves_core` (zero duplicação)
- `docs/email-marketing/daily_waves.py` - 2 pontos editados (bloco de import + `refresh_healthy`),
  9 linhas no diff (4 inserções + 5 deleções — colapso do bloco de 4 linhas em 3), nenhuma outra
  linha tocada

## Decisions Made

- **Dois módulos, não um** — decisão de discrição do CONTEXT.md exercida conforme o plano previu:
  `waves_lib.py` declara no docstring o contrato "funções puras/testáveis offline"; misturar
  `login()`/`fetch_leads()` (rede) ali quebraria isso. `waves_core.py` (infra com I/O) e
  `waves_copy.py` (template+copy) resolvem sem tocar em `waves_lib.py`.
- **Cópia, não movimentação** — os 3 arquivos fonte (`fire_today_20.py`/`fire_w1_2707.py`/
  `fire_w2w3_2707.py`) seguem no disco intocados; o rollback deste plano, se necessário, é reverter
  1 commit de import, não recuperar um arquivo deletado. Arquivamento é 10-04, depois que 10-03
  religar os 6 dependentes vivos não-`fire_*`.

## Deviations from Plan

None - plan executed exactly as written. Todas as `<action>`/`<verify>`/`<acceptance_criteria>` de
cada task foram seguidas e verificadas individualmente (ver evidência acima e Self-Check abaixo).

## Issues Encountered

Nenhum bloqueio. O `--dry --force` rodou com rede real (login OK, fetch_leads OK, site_check OK) e
completou sem erro — não foi necessário registrar mensagem de falha de API/login como previsto no
critério de aceite alternativo do Task 3.

## User Setup Required

None - nenhuma configuração externa necessária. O dry-run usou credenciais já presentes em
`~/.claude/.secrets.env` (login funcionou normalmente).

## Next Phase Readiness

- `waves_core.py`/`waves_copy.py` estão prontos e provados equivalentes ao estado pré-refactor —
  plano 10-03 pode religar os 6 dependentes vivos não-`fire_*` (`replenish_leads.py`,
  `import_extrator_bridge.py`, `rescrape_21.py`, `rescrape_27.py`, `rescrape_tavily_27.py`,
  `scrape_and_import.py`, `debug_campaign.py`) apontando para os módulos novos
- `fire_today_20.py`/`fire_w1_2707.py`/`fire_w2w3_2707.py` continuam no disco, inalterados,
  funcionando como fallback até o 10-04 arquivá-los — só podem ir para `archive/` depois que 10-03
  confirmar os 7 dependentes vivos religados e retestados
- `docs/email-marketing/reply_watch.py` segue com mudanças não commitadas (trabalho em andamento
  não relacionado, per STATE.md Blockers) e foi corretamente deixado intocado por este plano

## Self-Check: PASSED

- FOUND: docs/email-marketing/waves_core.py
- FOUND: docs/email-marketing/waves_copy.py
- FOUND: docs/email-marketing/daily_waves.py (modificado)
- FOUND: 37d19c53 (Task 1 commit)
- FOUND: 28b83aa8 (Task 2 commit)
- FOUND: bed2bc73 (Task 3 commit)

---
*Phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook*
*Completed: 2026-09-19*
