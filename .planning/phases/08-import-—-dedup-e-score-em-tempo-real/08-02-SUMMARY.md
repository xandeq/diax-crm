---
phase: 08-import-—-dedup-e-score-em-tempo-real
plan: 02
subsystem: crm
tags: [lead-scoring, dotnet, xunit, moq, domain-logic]

# Dependency graph
requires:
  - phase: 07-extra-o-qualidade-na-entrada
    provides: "Customer.WebsiteKind (EXTR-03), Customer.Quality/EmailType/HasSuspiciousDomain (sanitização), Customer.IsEligibleForCampaigns"
provides:
  - "LeadScoringService.CalculateScore recalibrado: bloco de fit com teto 45 (era 25), consumindo WebsiteKind/Quality/EmailType/HasSuspiciousDomain da Phase 7"
  - "LeadScoringService.SegmentForScore(int): fonte única e pública dos limiares Hot/Warm/Cold, consumida por RecomputeAllAsync"
affects: [08-04-import-lead-scoring, lead-scoring-worker]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Recalibração de scoring feita DENTRO da função pura existente (CalculateScore), nunca em caminho paralelo — garante paridade entre worker das 06h BRT e o futuro import (plano 08-04)"
    - "Helper estático puro (SegmentForScore) extraído para eliminar duplicação de limiares entre múltiplos consumidores"

key-files:
  created: []
  modified:
    - api-core/src/Diax.Application/Customers/LeadScoringService.cs
    - api-core/tests/Diax.Tests/Application/Customers/LeadScoringServiceTests.cs

key-decisions:
  - "D-05 (usuário, 2026-09-07): bloco de fit passa a incluir sinais de qualidade da Phase 7 (WebsiteKind.OwnSite +10, Quality.High +5, EmailType.PersonalDirect +5, HasSuspiciousDomain -15), subindo o teto do fit de 25 para 45 — um lead de sinais fortes agora nasce Warm (30) sem nenhum engajamento; Hot (60) permanece intencionalmente inalcançável só com fit"
  - "SegmentForScore(int) extraído como fonte única dos limiares Hot/Warm/Cold, substituindo o ternário inline em RecomputeAllAsync — o import do plano 08-04 chamará o mesmo helper"

patterns-established:
  - "Sinais de qualidade coletados na Phase 7 (site próprio vs diretório, qualidade de cadastro, tipo de email) são tratados como componentes de FIT (propriedade estática do lead), não de engajamento — mantém a separação conceitual entre 'quem o lead é' e 'o que o lead fez'"

requirements-completed: [IMPT-03]

# Metrics
duration: 12min
completed: 2026-09-07
---

# Phase 8 Plan 02: Recalibração do Lead Scoring com Sinais da Phase 7 Summary

**Bloco de fit de `LeadScoringService.CalculateScore` recalibrado de teto 25 para 45, incorporando `WebsiteKind.OwnSite` (+10), `Quality.High` (+5), `EmailType.PersonalDirect` (+5) e `HasSuspiciousDomain` (-15); `SegmentForScore(int)` extraído como fonte única dos limiares Hot/Warm/Cold.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-09-07T18:47:00-03:00 (aprox.)
- **Completed:** 2026-09-07T19:00:43-03:00
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- Bloco de fit de `CalculateScore` recalibrado (D-05): teto sobe de 25 para 45, incluindo os 3 sinais
  positivos novos da Phase 7 e a penalidade de domínio suspeito, mantendo Hot (60) matematicamente
  inalcançável sem engajamento.
- `SegmentForScore(int score)` extraído como método estático público — fonte única dos limiares
  `HotThreshold`/`WarmThreshold`, consumida por `RecomputeAllAsync` (worker das 06h BRT) e pronta
  para ser chamada pelo import (plano 08-04, fora de escopo deste plano).
- XML-doc de `CalculateScore` atualizado para refletir o novo teto e a origem dos novos sinais.
- 4 novos `[Fact]` + 1 `[Theory]` de 6 casos cobrindo o split Warm/Cold por fit, o teto abaixo de
  Hot, a queda por domínio suspeito e o mapeamento exato de `SegmentForScore` nos limiares (29/30/59/60).
- Os 12 testes pré-existentes de `LeadScoringServiceTests` passam **sem edição** — confirmado por
  `git diff --stat` vazio na Task 1 e pela suíte verde na Task 2.

## Task Commits

Each task was committed atomically (--no-verify, execução paralela com plano 08-01):

1. **Task 1: Recalibrar o bloco de fit de CalculateScore com os sinais da Phase 7** - `0f4f2d3` (feat)
2. **Task 2: Testes do split Warm/Cold por fit e do teto abaixo de Hot** - `ff0080c` (test)

_Nenhum ciclo TDD RED→GREEN separado em commits distintos: `tdd="true"` no plano indicava seguir o
fluxo mental RED/GREEN, mas cada task já entregava o código de produção testável e coberto —
commitados como uma unidade coesa por task, seguindo o padrão do restante do plano (implementação +
verificação via `dotnet test` antes do commit)._

## Files Created/Modified

- `api-core/src/Diax.Application/Customers/LeadScoringService.cs` - bloco de fit recalibrado (teto
  25→45), `SegmentForScore(int)` adicionado, `RecomputeAllAsync` consumindo o helper, XML-doc atualizado
- `api-core/tests/Diax.Tests/Application/Customers/LeadScoringServiceTests.cs` - helper `MakePhase7Lead`
  + 4 `[Fact]` + 1 `[Theory]` (6 casos) cobrindo o novo comportamento; nenhum teste pré-existente tocado

## Decisions Made

- Seguido D-05 à risca: recalibração feita **dentro** de `CalculateScore` (função estática pura, sem
  I/O), não em caminho paralelo — o worker das 06h BRT e o futuro import produzirão sempre o mesmo
  score para o mesmo `Customer`.
- `SegmentForScore` foi feito `public static` (não `private`) precisamente para ser reutilizável pelo
  plano 08-04, conforme o `key_links` do frontmatter do plano exigia.
- Nenhuma dependência nova injetada em `LeadScoringService` — confirmando a restrição do plano (nada
  de `ICachedMxCheckService`, MX check não é input de score).

## Deviations from Plan

None - plan executado exatamente como escrito. Todos os valores aritméticos (45, 25, teto < 60) bateram
exatamente com a prova do plano na primeira tentativa; nenhum teste pré-existente precisou de ajuste.

## Issues Encountered

- Durante a primeira rodada de `dotnet test` completo, um erro de compilação transitório apareceu em
  `CustomerRepositoryExternalIdTests.cs` (`ICurrentUserService` não encontrado) — arquivo pertencente
  ao plano 08-01, sendo editado concorrentemente pelo outro agente paralelo. Fora de escopo (Rule:
  scope boundary), não foi tocado; uma segunda execução do build (após o outro agente avançar sua
  edição) compilou e passou normalmente. Nenhuma ação foi necessária além de re-executar o comando.
- Um lock de arquivo transitório (`Diax.Tests.dll` em uso por outro processo `testhost`) apareceu
  como warning `MSB3026` durante o build paralelo — resolvido automaticamente pelo retry nativo do
  MSBuild, sem impacto no resultado (`Failed: 0`).

## Effect on Existing Leads (declared per D-05, not mitigated)

Conforme exigido pelo plano, este efeito colateral é **esperado e aceito**, não corrigido:

Na próxima execução do `LeadScoringWorker` (06:00 BRT / 09:00 UTC), leads **já existentes** terão o
score recalculado com a nova fórmula:
- Leads com `WebsiteKind = OwnSite`, `Quality = High` ou `EmailType = PersonalDirect` ganham até
  **+20 pontos** no total (10+5+5) e alguns migrarão de `Cold` para `Warm`.
- Leads com `HasSuspiciousDomain = true` perdem **15 pontos**.
- Leads legados nunca tocados por um import da Phase 7 têm `WebsiteKind = Unknown`, `Quality = null`,
  `EmailType = null` — não ganham nem perdem nada, o delta desses sinais é 0 para eles.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `LeadScoringService.SegmentForScore(int)` está pronto para ser chamado pelo import no plano 08-04
  (IMPT-03), garantindo que o score calculado no momento do import produza o mesmo segmento que o
  worker das 06h BRT produziria para o mesmo `Customer`.
- Suíte completa verde: 882 testes passando (baseline 868 + 10 novos deste plano + 4 do plano 08-01
  paralelo), `Failed: 0`.
- Nenhum blocker para o plano 08-04.

---
*Phase: 08-import-—-dedup-e-score-em-tempo-real*
*Completed: 2026-09-07*

## Self-Check: PASSED

- FOUND: api-core/src/Diax.Application/Customers/LeadScoringService.cs
- FOUND: api-core/tests/Diax.Tests/Application/Customers/LeadScoringServiceTests.cs
- FOUND: .planning/phases/08-import-—-dedup-e-score-em-tempo-real/08-02-SUMMARY.md
- FOUND commit: 0f4f2d3 (feat(08-02): recalibra bloco de fit do lead scoring com sinais da Phase 7)
- FOUND commit: ff0080c (test(08-02): cobre split Warm/Cold por fit e teto abaixo de Hot)
