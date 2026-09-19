---
phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook
plan: 03
subsystem: email-marketing
tags: [python, refactor, production-critical, email-outreach, scraping]

# Dependency graph
requires:
  - phase: 10-02
    provides: "waves_core.py/waves_copy.py criados e provados equivalentes ao fire_today_20/fire_w1_2707/fire_w2w3_2707"
provides:
  - "Os 7 dependentes vivos de fire_today_20.py (import_extrator_bridge.py, replenish_leads.py, rescrape_21.py, rescrape_27.py, rescrape_tavily_27.py, scrape_and_import.py, debug_campaign.py) religados em waves_core.py"
  - "Grep de dependencia viva (from fire_|import fire_, excluindo os proprios fire_*.py) retorna vazio — pre-requisito D-03 do 10-04 satisfeito"
affects: [10-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Rewire de 1 linha de import por arquivo, isolando o arquivo de maior risco (bridge extrator->CRM) em sua propria task com prova de equivalencia dry-run antes/depois"
    - "Verificacao sem execucao para modulo com efeito colateral no import (debug_campaign.py): py_compile + AST estatico, nunca import real"

key-files:
  created: []
  modified:
    - docs/email-marketing/import_extrator_bridge.py
    - docs/email-marketing/replenish_leads.py
    - docs/email-marketing/rescrape_21.py
    - docs/email-marketing/rescrape_27.py
    - docs/email-marketing/rescrape_tavily_27.py
    - docs/email-marketing/scrape_and_import.py
    - docs/email-marketing/debug_campaign.py
    - docs/email-marketing/waves_smoke.py

key-decisions:
  - "import_extrator_bridge.py tratado em task isolada (Task 1) com dry-run antes/depois capturado no scratchpad, por ser a ponte de producao extrator->CRM de maior risco do plano"
  - "debug_campaign.py alterado via Edit de texto puro, jamais importado/executado — verificado so com py_compile + parse AST estatico checando o nome do modulo importado, exatamente como o plano exigiu"
  - "waves_smoke.py teve um comentario historico corrigido (mencionava `import fire_today_20 as ft` de debug_campaign.py, que causava falso-positivo no grep-gate `from fire_|import fire_` usado pelo 10-04) — fix de escopo minimo, nao mexe em nenhuma logica de check"

requirements-completed: [SCRP-01]

# Metrics
duration: ~20min
completed: 2026-09-19
---

# Phase 10 Plan 03: Religar os 7 Dependentes Vivos em waves_core Summary

**Os 7 últimos consumidores vivos de `fire_today_20.py` (incluindo a ponte extrator→CRM de produção e o disparo automático de reposição de estoque de sexta) foram religados em `waves_core.py`, com prova de equivalência dry-run e o grep de dependência viva zerado — liberando o arquivamento do 10-04.**

## Performance

- **Duration:** ~20 min
- **Completed:** 2026-09-19T22:24:32Z
- **Tasks:** 3
- **Files modified:** 8 (7 rewires de 1 linha + 1 correção de comentário em waves_smoke.py)

## Accomplishments

- `import_extrator_bridge.py` (Task 1, isolado): linha 23 trocada para `from waves_core import
  load_env, login, base`. Dry-run antes/depois idêntico (SSH para a VPS falhou com
  `Connection timed out` nas DUAS execuções — equivalência de comportamento confirmada, não é
  regressão introduzida por este plano). Bindings provados: `m.load_env.__module__ == 'waves_core'`,
  `m.login.__module__ == 'waves_core'`, `m.base == 'https://api.alexandrequeiroz.com.br'`. `git diff`
  mostra exatamente 1 linha alterada.
- `replenish_leads.py` (Task 2): linha 13 trocada. `run(cap=120)` preservado (assinatura
  `(cap=120)` confirmada via `inspect.signature`), `email_ok` LOCAL do arquivo permanece intacto
  (`r.email_ok.__module__ == 'replenish_leads'`, não confundido com o de `waves_core`). `run()`
  nunca executado durante a verificação — só import + inspect.
- 5 utilitários restantes (Task 3): `rescrape_21.py`, `rescrape_27.py`, `rescrape_tavily_27.py`,
  `scrape_and_import.py` religados (`from waves_core import load_env, login, base, ts`);
  `debug_campaign.py` religado (`import waves_core as ft`) SEM jamais ser importado ou executado —
  verificado só com `py_compile` (exit 0) + análise AST estática confirmando `waves_core` na lista
  de imports e `fire_today_20` ausente, mais `hasattr(waves_core, 'build_html')` (o único símbolo
  que `debug_campaign.py` consome).
- Grep final de fechamento do plano, `grep -rn "from fire_\|import fire_" --include=*.py . | grep -v
  "^./fire_"` (a partir de `docs/email-marketing/`): **retorna vazio** — zero dependente vivo aponta
  para `fire_today_20`/`fire_w1_2707`/`fire_w2w3_2707`. Pré-requisito D-03 do 10-04 satisfeito.
- `waves_smoke.py` → `5/5 OK` após cada task (Task 1, 2 e 3), sem regredir.
- `daily_waves.py --dry --force` rodou até `===== FIM =====` com rede real, sem traceback — plano
  de 20 nichos construído normalmente após todos os 7 rewires.
- `fire_today_20.py`/`fire_w1_2707.py`/`fire_w2w3_2707.py` continuam no disco, intocados, nada
  neste plano os moveu ou apagou.

## Task Commits

Each task was committed atomically:

1. **Task 1: Religar a ponte extrator→CRM (import_extrator_bridge.py) — isolado e verificado de ponta a ponta** - `1b6795d` (feat)
2. **Task 2: Religar replenish_leads.py (caminho vivo de sexta-feira do daily_waves)** - `6193294` (feat)
3. **Task 3: Religar os 5 utilitários restantes e provar que nenhum vivo depende mais de fire_*** - `f45a228` (feat)

## Files Created/Modified

- `docs/email-marketing/import_extrator_bridge.py` - ponte extrator→CRM de produção, 1 linha
  (import) trocada para `waves_core`
- `docs/email-marketing/replenish_leads.py` - reposição de estoque de sexta, 1 linha trocada
- `docs/email-marketing/rescrape_21.py`, `rescrape_27.py`, `rescrape_tavily_27.py`,
  `scrape_and_import.py` - utilitários ad-hoc de scraping, 1 linha cada
- `docs/email-marketing/debug_campaign.py` - ferramenta de debug de templates de email, 1 linha
  (`import waves_core as ft`), nunca executada
- `docs/email-marketing/waves_smoke.py` - comentário corrigido (referência histórica a
  `fire_today_20` no docstring de `EXCLUDE_PREFIXES`, reescrita para não colidir com o grep-gate
  literal `from fire_|import fire_` usado pelo 10-04)

## Decisions Made

- **`import_extrator_bridge.py` em task isolada** — conforme o plano exigiu, tratado sozinho com
  verificação própria de ponta a ponta (dry-run antes/depois + bindings), por ser o arquivo de maior
  risco (ponte de produção extrator→CRM).
- **`debug_campaign.py` nunca importado nem executado** — único critério de aceite não-negociável
  deste plano; verificação feita 100% via `py_compile` + AST estático, exatamente como especificado.
- **Correção do comentário em `waves_smoke.py`** — descoberta durante a verificação do gate final
  (Task 3): o comentário original citava literalmente `import fire_today_20 as ft` (comportamento
  antigo de `debug_campaign.py`), o que fazia o próprio `waves_smoke.py` aparecer como falso-positivo
  no grep `from fire_\|import fire_` que o 10-04 usa como gate mecânico de arquivamento. Corrigido
  para descrever o padrão sem reproduzir a string literal — fix de escopo mínimo (Rule 3: bloqueio
  do próprio critério de aceite do plano), sem tocar em nenhuma lógica de check.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Comentário em waves_smoke.py causava falso-positivo no grep-gate final**
- **Found during:** Task 3, ao rodar o grep de fechamento do plano
- **Issue:** `waves_smoke.py` tinha um comentário citando literalmente `import fire_today_20 as ft`
  (documentando o comportamento antigo de `debug_campaign.py`, já corrigido nesta mesma task). Esse
  texto fazia `grep -rn "from fire_\|import fire_"` encontrar uma linha em `waves_smoke.py`, que não
  é um arquivo `fire_*` e portanto não era filtrado pelo `grep -v "^./fire_"` — quebrando o critério
  de aceite explícito do plano ("grep... retorna VAZIO") e o gate mecânico que o 10-04 depende.
- **Fix:** Reescrito o comentário para descrever o histórico sem reproduzir a string literal
  `import fire_` (usa "alias de `fire_today_20`" em vez de citar a linha de código exata).
- **Files modified:** `docs/email-marketing/waves_smoke.py`
- **Verification:** `grep -rn "from fire_\|import fire_" --include=*.py . | grep -v "^./fire_"`
  retorna vazio; `python waves_smoke.py` continua `5/5 OK`.
- **Committed in:** `f45a228` (Task 3 commit)

**2. [Rule N/A - considerado e revertido] Docstring de import_extrator_bridge.py mencionando fire_today_20**
- **Found during:** Task 1, verificação de aceite
- **Issue:** O critério de aceite "grep -n 'fire_today_20' ... retorna ZERO linhas" e o critério
  "git diff mostra EXATAMENTE 1 linha alterada" são mutuamente incompatíveis para este arquivo — a
  linha 16 do docstring menciona "fire_today_20 (login/base)" como referência histórica de requisito.
  Editar essa linha satisfaria o primeiro critério mas violaria o segundo (mais rígido, "nenhuma
  outra alteração no arquivo", explícito para este arquivo de alto risco).
- **Decision:** Priorizado o critério "exatamente 1 linha" por ser o mais específico e crítico
  para um arquivo de produção classificado como maior risco do plano. A linha do docstring foi
  deixada como estava (não é um import real, não afeta a checagem funcional; o grep-gate real que
  importa para o 10-04 — `from fire_\|import fire_` — já não a captura, pois o texto é
  "fire_today_20 (login/base)", sem os prefixos "from fire_"/"import fire_").
- **Files modified:** nenhum (decisão de não-alteração)
- **Verification:** `git diff docs/email-marketing/import_extrator_bridge.py` mostra exatamente 1
  linha alterada; `grep -rn "from fire_\|import fire_"` (o gate real do 10-04) não captura essa
  linha do docstring.
- **Committed in:** N/A (não houve mudança de código para este item)

---

**Total deviations:** 1 auto-fixed (Rule 3 - blocking), 1 conflito de critérios de aceite resolvido
por decisão documentada (sem alteração de código).
**Impact on plan:** Ambos preservam a integridade do rewire de produção; nenhum scope creep.

## Issues Encountered

- SSH para `root@185.173.110.180:2282` falhou com `Connection timed out` nas duas execuções (antes e
  depois do rewire de `import_extrator_bridge.py`) — ambiente sem rota de rede até a VPS neste
  momento. Não é regressão: o comportamento é idêntico antes/depois, e o critério de aceite do plano
  já previa esse cenário explicitamente ("Se o SSH falhar nas DUAS execuções... isso conta como
  equivalência"). O caminho de import binding (que não depende de SSH) foi verificado com sucesso.

## User Setup Required

None - nenhuma configuração externa necessária.

## Next Phase Readiness

- Os 7 dependentes vivos de `fire_today_20.py` estão religados em `waves_core.py` e re-testados —
  o pré-requisito D-03 do 10-04 (arquivamento de `fire_today_20.py`/`fire_w1_2707.py`/
  `fire_w2w3_2707.py` e dos ~40 scripts `fire_wN_XX.py` mortos) está satisfeito.
- Grep de fechamento (`from fire_|import fire_`, excluindo os próprios `fire_*.py`) retorna vazio —
  o 10-04 pode prosseguir com o `mv` para `archive/` sem quebrar nenhum consumidor vivo.
- `import_extrator_bridge.py` não pôde ser validado ponta-a-ponta contra a VPS real neste ambiente
  (SSH sem rota) — recomenda-se ao 10-04 (ou a uma verificação manual futura) re-rodar
  `python import_extrator_bridge.py --dry` quando houver conectividade com `185.173.110.180:2282`
  para confirmar o resumo de importação completo, não só os bindings de módulo.
- `docs/email-marketing/reply_watch.py` segue com mudanças não commitadas (trabalho em andamento
  não relacionado a este milestone, per STATE.md Blockers) e foi corretamente deixado intocado por
  este plano.

## Self-Check: PASSED

- FOUND: docs/email-marketing/import_extrator_bridge.py (modificado, from waves_core import load_env, login, base)
- FOUND: docs/email-marketing/replenish_leads.py (modificado, from waves_core import load_env, login, base, ts)
- FOUND: docs/email-marketing/rescrape_21.py
- FOUND: docs/email-marketing/rescrape_27.py
- FOUND: docs/email-marketing/rescrape_tavily_27.py
- FOUND: docs/email-marketing/scrape_and_import.py
- FOUND: docs/email-marketing/debug_campaign.py (import waves_core as ft, nunca importado)
- FOUND: docs/email-marketing/waves_smoke.py (comentário corrigido)
- FOUND: 1b6795d (Task 1 commit)
- FOUND: 6193294 (Task 2 commit)
- FOUND: f45a228 (Task 3 commit)

---
*Phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook*
*Completed: 2026-09-19*
