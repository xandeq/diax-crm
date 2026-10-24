---
phase: 12-escala-de-email
plan: 02
subsystem: email-marketing
tags: [python, warmup, daily-waves, email-cadence, telegram]

# Dependency graph
requires:
  - phase: 10-motor-unico-de-cadencia
    provides: waves_core.py / daily_waves.py / waves_smoke.py como motor único, religado e testável offline
provides:
  - "WARMUP_TARGETS / warmup_target() / max_per_seg() / next_warmup_day() em waves_core.py — funções puras, testáveis offline"
  - "daily_waves.build_waves() dimensionado pelo alvo do dia (MAX_PER_SEG derivado do warmup, não de PER_PROVIDER fixo)"
  - "guard de fila diária (already_queued_today() >= target) em lockstep com o alvo — nunca mais trunca a rampa em 50"
  - "degrau de warmup só avança com >=80% de entrega no dia; senão segura e alerta no Telegram"
  - "6º check offline (warmup_curva) no waves_smoke.py cobrindo toda a curva"
affects: [email-marketing, lead-scoring-supply, phase-12-remaining-plans]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Funções puras de dimensionamento (warmup_target/max_per_seg/next_warmup_day) isoladas em waves_core.py para permitir smoke test offline sem rede/DB"
    - "State additivo: novas chaves (warmup_day, warmup_last_date, warmup_target_hoje, warmup_delivered) somadas ao previews/daily-state.json sem tocar nas existentes"

key-files:
  created: []
  modified:
    - docs/email-marketing/waves_core.py
    - docs/email-marketing/waves_smoke.py
    - docs/email-marketing/daily_waves.py

key-decisions:
  - "Curva de warmup literal [87,104,125,150,180,216,260,300] (D-03), não int(87*1.2**n) — evita estourar o teto de 300 no dia 8"
  - "Guard anti-duplicado (already_queued_today) passa a comparar contra o alvo do dia em vez do literal 50, eliminando o auto-sabotador identificado como Pitfall 4 na pesquisa da Phase 12"
  - "Degrau do warmup só avança 1x/dia (guardado por warmup_last_date) e exige >=80% de entrega do alvo; caso contrário segura o degrau e acende alerta no Telegram em vez de falhar em silêncio"

requirements-completed: [EMAL-02]

# Metrics
duration: ~15min
completed: 2026-09-20
---

# Phase 12 Plan 02: Warmup de Volume (87→300/dia) Summary

**Curva de warmup literal +20%/dia implementada como funções puras em waves_core.py, ligada ao dimensionamento e ao guard anti-duplicado de daily_waves.py, com déficit de estoque agora barulhento no Telegram em vez de silencioso.**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-09-20T16:02:53Z (início da fase, conforme STATE.md)
- **Completed:** 2026-09-20T16:09:04Z
- **Tasks:** 2/2 completas
- **Files modified:** 3 (`waves_core.py`, `waves_smoke.py`, `daily_waves.py`)

## Accomplishments

- `WARMUP_TARGETS`, `warmup_target(day)`, `max_per_seg(target, n_seg, n_prov, min_seg)` e
  `next_warmup_day(dia, entregue, alvo)` criados em `waves_core.py` como funções puras (sem rede/DB),
  testáveis offline pelo smoke — a curva 87→104→125→150→180→216→260→300 é literal, clampada em ambas
  as pontas.
- `build_waves()` agora calcula `target = warmup_target(st.get('warmup_day', 1))` e deriva
  `MAX_PER_SEG` de `max_per_seg(target, ...)`, logando `WARMUP dia N/8 — alvo X/dia` antes de montar
  o plano.
- **O bug identificado pela pesquisa (Pitfall 4) está corrigido**: o guard `already_queued_today() >=
  50` — que a partir do dia 3 da rampa cortaria as waves em silêncio — virou
  `already_queued_today() >= target`. Confirmado por grep: `guard >= 50` zero ocorrências,
  `guard >= target` exatamente 1.
- O degrau só avança se o dia entregou ≥80% do alvo (`WARMUP_KEEP_RATIO`), avalia no máximo uma vez
  por dia (`warmup_last_date`), e o resumo do Telegram passou a mostrar `Agendados hoje: N / alvo X
  (warmup dia D/8)` mais um alerta `📉 ABAIXO DO ALVO` quando a entrega fica abaixo de 80% — a
  "warning sign" que a pesquisa pediu para tornar visível em vez de sumir.
- `waves_smoke.py` ganhou o 6º check (`warmup_curva`), cobrindo literalmente todas as assertivas do
  plano; docstring atualizada de "5 checks" para "6 checks".

## Task Commits

Each task was committed atomically:

1. **Task 1: Curva de warmup como função pura em waves_core.py + 6º check no smoke** - `6f449e2c` (feat)
2. **Task 2: Ligar o alvo do dia em build_waves — dimensionamento, guard em lockstep e alvo-vs-entregue no resumo** - `657666ee` (feat)

**Plan metadata:** (this commit) — `docs(12-02): complete Warmup de Volume plan`

## Files Created/Modified

- `docs/email-marketing/waves_core.py` — bloco de warmup (`WARMUP_TARGETS`, `warmup_target`,
  `max_per_seg`, `next_warmup_day`), `PER_PROVIDER` preservado intacto
- `docs/email-marketing/waves_smoke.py` — check `warmup_curva` (6º), contador da docstring atualizado
- `docs/email-marketing/daily_waves.py` — import do bloco de warmup, `build_waves()` dimensionado
  pelo alvo, guard em lockstep, avanço honesto do degrau, resumo do Telegram com alvo-vs-entregue

## Decisions Made

- Curva literal (não fórmula `int(87*1.2**n)`) — decisão D-03 já travada no CONTEXT.md, confirmada
  em código: `int(87*1.2**n)` daria `87,104,125,150,180,215,259,311` e estouraria o teto de 300 no
  último dia.
- Avanço do degrau limitado a uma avaliação por dia (`warmup_last_date`) — protege contra a task
  rodando duas vezes no mesmo dia e pulando dois degraus.
- Alerta de estoque baixo no Telegram usa o mesmo threshold de 80% do `next_warmup_day` (consistência
  entre "segura o degrau" e "avisa o usuário").

## Deviations from Plan

None — plano executado exatamente como escrito. Todas as 3 acceptance criteria automatizadas da
Task 1 e todas as 7 da Task 2 foram verificadas literalmente (ver seção "Verificação ao vivo"
abaixo).

## Issues Encountered

None.

## Verificação ao vivo (medido nesta execução, 2026-09-20)

**Volume real de envio (remedido, igual ao achado ao vivo do plano):**

| Janela | enviados (status=2) |
|--------|---------------------|
| 7d  | 59 |
| 14d | 260 (~18,6/dia) |
| 30d | 936 (~31/dia) |

**Alvo calculado para o dia 1 do warmup:** 87/dia (`warmup_target(1)`), com
`MAX_PER_SEG = max_per_seg(87, n_seg=12, n_prov=4) = 8` leads por nicho tentado.

**Saída relevante do `--dry --force` (log `logs/daily-waves-2026-09-20.log`):**
```
WARMUP dia 1/8 — alvo 87/dia · 4 provider(s) · MAX_PER_SEG=8
DRY: alvo do dia = 87 (warmup dia 1)
DRY: plano = [('marketing', 4), ('consultoria', 4)]
```

**Avaliação honesta do gargalo de estoque:** rodando a rotação completa de 20 nichos com
`MAX_PER_SEG=8` (o piso da curva, dia 1/8), **apenas 2 dos 20 nichos** (`marketing` e `consultoria`)
tiveram estoque suficiente (≥`MIN_SEG=4`) para justificar campanha própria — os outros 18 caíram
abaixo do piso (ex.: `construtora: 0`, `advocacia: 0`, `otica: 1`, `medico: 1`). Mesmo no degrau
**mais baixo** do warmup (87/dia), o mecanismo entregaria muito abaixo do alvo por pura falta de
lead nunca-contactado por nicho — exatamente o gargalo que o objetivo do plano já apontava (achado
ao vivo de 2026-09-20): o motor de rampa está correto e funcional, mas o teto real hoje não é o
código, é o estoque de `customers` sem contato prévio segmentado por nicho. Isso agora fica visível
tanto no log quanto no alerta `📉 ABAIXO DO ALVO` do Telegram em execuções reais (não-dry), em vez de
sumir silenciosamente como antes (Pitfall 4). Resolver o estoque é trabalho de outro plano/fase
(reposição de leads por nicho via Tavily já existe em `replenish_leads.py`, rodado às sextas) — fora
do escopo deste plano.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- O motor sabe seu alvo diário, o guard não trunca mais a rampa, e o degrau só avança com entrega
  real — pronto para os próximos planos da Phase 12 que dependem deste mecanismo (12-04, sequenciado
  via `depends_on` após este plano).
- Concern para o usuário/roadmap: o warmup até 300/dia é mecanicamente correto, mas não vai ser
  observado na prática até o estoque de leads nunca-contactados por nicho crescer — próximo gargalo
  real da Phase 12 é reposição/segmentação de leads, não mais o guard de volume.

---
*Phase: 12-escala-de-email*
*Completed: 2026-09-20*

## Self-Check: PASSED

All created/modified files confirmed present on disk; both task commits (`6f449e2`, `657666e`)
confirmed in `git log`.
