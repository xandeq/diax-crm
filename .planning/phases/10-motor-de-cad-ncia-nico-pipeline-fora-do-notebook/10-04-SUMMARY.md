---
phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook
plan: 04
subsystem: infra
tags: [python, email-outreach, script-consolidation, cleanup]

requires:
  - phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook (10-02, 10-03)
    provides: waves_core.py/waves_copy.py extraidos verbatim; 7 dependentes vivos religados; grep de dependencia de fire_* zerado
provides:
  - Os 48 fire_*.py arquivados em docs/email-marketing/archive/ (6 tracked via rename, 42 untracked novos), raiz de docs/email-marketing/ sem nenhum fire_*.py
  - archive/README.md com aviso de conteudo historico nao-executavel, mapa simbolo->modulo novo, instrucao de como criar campanha nova
  - MIGRACAO-fire-scripts.md fechado como registro do que foi feito (status CONCLUIDO, 7 imports atuais colados, secao de rollback, evidencia final do --dry --force)
  - SCRP-01 marcado Complete na traceability de REQUIREMENTS.md (ultimo dos 3 planos implementadores: 10-02/10-03/10-04)
affects: [phase-11-qualified-objetivo, phase-12-escala-email]

tech-stack:
  added: []
  patterns: ["arquivamento por padrao de nome (fire_*.py), nao por status no git", "mv de sistema de arquivos + git add scoped (nunca git add -A na raiz) para deixar o git detectar rename automaticamente em arquivos tracked"]

key-files:
  created:
    - docs/email-marketing/archive/README.md
    - docs/email-marketing/archive/ (48 scripts fire_*.py movidos)
    - .planning/phases/10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook/10-04-SUMMARY.md
  modified:
    - docs/email-marketing/MIGRACAO-fire-scripts.md
    - .planning/REQUIREMENTS.md
    - .planning/STATE.md
    - .planning/ROADMAP.md

key-decisions:
  - "Checkpoint humano (Task 1) reconferiu as 4 pre-condicoes ao vivo (grep vazio, waves_smoke 5/5, import_extrator_bridge --dry com erro de SSH esperado - nao ImportError -, contagem 48) antes de aprovar o move dos 48 arquivos"
  - "6 dos 48 fire_*.py sao tracked no git (fire_lote34.py, fire_today_19.py, fire_today_20.py, fire_w1_2707.py, fire_w2w3_2707.py, fire_wave_today.py), nao 3 como estimado no checkpoint - usado mv de sistema de arquivos + git add scoped em ambos os grupos (tracked e untracked), deixando o git detectar o rename automaticamente para os tracked, em vez de forcar git mv"
  - "DRY evidence do daily_waves.py --dry --force reaproveitada da reverificacao do Task 2 (mesmo estado pos-archive, sem mudanca de codigo entre Task 2 e Task 3) em vez de reexecutar identicamente no Task 3"

requirements-completed: [SCRP-01]

duration: 25min
completed: 2026-09-19
---

# Phase 10 Plan 04: Arquivamento dos fire_*.py Summary

**48 scripts fire_*.py arquivados em docs/email-marketing/archive/ (6 via git rename, 42 como novos untracked), com README de aviso e motor reverificado pos-move — fecha SCRP-01.**

## Performance

- **Duration:** ~25 min (retomado apos checkpoint aprovado)
- **Tasks:** 3 (Task 1 checkpoint ja aprovado pelo usuario antes desta execucao; Task 2 e Task 3 executados nesta sessao)
- **Files modified:** 52 (48 fire_*.py movidos + archive/README.md + MIGRACAO-fire-scripts.md + REQUIREMENTS.md)

## Accomplishments

- Todos os 48 `fire_*.py` movidos de `docs/email-marketing/` para `docs/email-marketing/archive/` — raiz limpa, nenhum deletado
- `archive/README.md` criado com aviso de nao-executavel, mapa de proveniencia do codigo (`fire_today_20`/`fire_w1_2707`/`fire_w2w3_2707` → `waves_core.py`/`waves_copy.py`) e instrucao de como criar campanha nova sem copiar arquivo
- Motor (`daily_waves.py`) e ponte extrator→CRM (`import_extrator_bridge.py`) reverificados DEPOIS do arquivamento — nenhuma quebra
- `MIGRACAO-fire-scripts.md` transformado de plano em registro do que foi feito, com rollback documentado
- `SCRP-01` fechado — ultimo requirement pendente da Phase 10

## Task Commits

1. **Task 1: Aprovacao humana da lista de arquivamento** - checkpoint aprovado pelo usuario em sessao anterior ("Aprovado, move os 48 (Recomendado)"), zero escrita em disco, nada commitado
2. **Task 2: Mover os 48 fire_*.py para archive/ e reverificar o motor** - `29d23e97` (feat)
3. **Task 3: Fechar o doc de migracao e registrar o estado final** - `220210aa` (docs, inclui tambem REQUIREMENTS.md)

_Nota: o commit do Task 3 agrupa a atualizacao de `MIGRACAO-fire-scripts.md` e `REQUIREMENTS.md` (SCRP-01), pois ambos fazem parte do fechamento documental do plano._

## Provas do Checkpoint (Task 1, reconferidas ao vivo antes do move)

1. `grep -rn "from fire_\|import fire_" --include=*.py . | grep -v "^./fire_"` → **saida vazia** (exit 1, nenhuma linha)
2. `python waves_smoke.py` → `== 5/5 OK ==`
3. `python import_extrator_bridge.py --dry` → erro de SSH/timeout de rede (ambiente sem rota ate a VPS, mesmo achado do 10-03), **nao** `ImportError`/`ModuleNotFoundError`
4. `ls fire_*.py | wc -l` → **48**

**Resposta do usuario:** "Aprovado, move os 48 (Recomendado)" — lista completa aprovada sem exclusoes.

## Contagem e md5 pos-move

- `ls docs/email-marketing/fire_*.py` → nenhum arquivo (raiz limpa)
- `ls docs/email-marketing/archive/fire_*.py | wc -l` → **48**
- md5 dos 3 fontes de extracao, identico antes e depois do move:

| Arquivo | md5 (antes = depois) |
|---|---|
| `fire_today_20.py` | `a32493625f498ae25452862b59a2a7be` |
| `fire_w1_2707.py` | `2e9f1e68429541a6d7ba95e8d66b055d` |
| `fire_w2w3_2707.py` | `e777ee6fed7c02eeee3266028eec5d18` |

(mesmos valores registrados no `10-02-SUMMARY.md` — nenhuma edicao durante o arquivamento)

## Reverificacao pos-move

```
python waves_smoke.py                     → == 5/5 OK ==
import import_extrator_bridge (--dry)     → bridge ok https://api.alexandrequeiroz.com.br
import replenish_leads                    → replenish ok True
py_compile debug_campaign.py              → COMPILE_OK (nunca importado/executado)
python daily_waves.py --dry --force       → DRY: plano = [('marketing', 4), ('consultoria', 4)]
                                             ===== FIM ===== (sem traceback)
```

## Files Created/Modified

- `docs/email-marketing/archive/README.md` - aviso de conteudo historico, mapa simbolo→modulo, como criar campanha nova
- `docs/email-marketing/archive/*.py` (48 arquivos) - fire_*.py movidos verbatim (6 via git rename, 42 como novos untracked)
- `docs/email-marketing/MIGRACAO-fire-scripts.md` - fechado: status CONCLUIDO, 7 linhas de import atual (todas `waves_core`), contagem confirmada, secao de rollback, evidencia final
- `.planning/REQUIREMENTS.md` - SCRP-01 marcado `[x]` Complete na secao de requirements e na tabela de traceability

## Decisions Made

- **6 tracked, nao 3:** o checkpoint estimava 3 arquivos tracked no git para usar `git mv`. `git ls-files` revelou 6 (`fire_lote34.py`, `fire_today_19.py`, `fire_today_20.py`, `fire_w1_2707.py`, `fire_w2w3_2707.py`, `fire_wave_today.py`). Resolvido com `mv` de sistema de arquivos seguido de `git add` escopado nos caminhos de origem/destino (nunca `git add -A` na raiz) — o git detectou os 6 renames automaticamente (`R` no `git status`), exatamente como o plano previa para os "eventuais outros" tracked alem dos 3 citados.
- **DRY evidence reaproveitada:** a saida `DRY: plano = [...]` colada no `MIGRACAO-fire-scripts.md` (Task 3) e a mesma execucao capturada na reverificacao do Task 2, ja que nenhum codigo mudou entre as duas tasks — evita reexecucao redundante que produziria o mesmo resultado.

## Deviations from Plan

None - plan executado exatamente como escrito. A unica divergencia (6 vs. 3 arquivos tracked) e um detalhe de mecanica ja antecipado pelo proprio plano ("e eventuais outros"), nao uma mudanca de escopo.

## Issues Encountered

None.

## Next Phase Readiness

- **Phase 10 completa** (5/5 planos: 10-01, 10-02, 10-03, 10-04, 10-05) — SCRP-01 e OBSV-01 ambos fechados.
- Phase 11 (Qualified Objetivo e Automatico) depende desta fase estar pronta — motor confiavel fora do notebook e pre-requisito para automatizar `reply_watch.py` com seguranca. Nenhum bloqueio conhecido.
- Phase 12 (Escala de Email) tambem depende desta fase — motor unico com script sprawl eliminado e pre-requisito para escalar volume com warmup real.

---
*Phase: 10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook*
*Completed: 2026-09-19*

## Self-Check: PASSED

- FOUND: docs/email-marketing/archive/README.md
- FOUND: docs/email-marketing/archive/fire_today_20.py
- FOUND: 48 archived fire_*.py files in archive/
- FOUND: .planning/phases/10-motor-de-cad-ncia-nico-pipeline-fora-do-notebook/10-04-SUMMARY.md
- FOUND: commit 29d23e9 (Task 2)
- FOUND: commit 220210a (Task 3)
