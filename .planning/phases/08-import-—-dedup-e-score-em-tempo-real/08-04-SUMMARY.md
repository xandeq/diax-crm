---
phase: 08-import-—-dedup-e-score-em-tempo-real
plan: 04
subsystem: api
tags: [lead-scoring, customer-import, dotnet, xunit, moq]

# Dependency graph
requires:
  - phase: 08-import-—-dedup-e-score-em-tempo-real
    plan: 02
    provides: "LeadScoringService.CalculateScore recalibrado (teto de fit 45) + SegmentForScore(int) extraido como fonte unica dos limiares"
  - phase: 08-import-—-dedup-e-score-em-tempo-real
    plan: 03
    provides: "Ramo CREATE/ENRICH reescrito no loop de persistencia de CustomerImportService.ImportAsync (resolucao ExternalId->email->telefone), ponto de insercao estavel apos UpdateClassification"
provides:
  - "Todo Customer criado pelo import (qualquer origem) nasce com LeadScore e Segment preenchidos, calculados pela mesma LeadScoringService.CalculateScore/SegmentForScore que o LeadScoringWorker das 06:00 BRT usa"
  - "Ramo ENRICH permanece explicitamente sem recalculo — comentario de guarda no codigo para nao ser 'completado' por engano depois"
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Chamada estatica de LeadScoringService.CalculateScore/SegmentForScore direto no ramo CREATE, sem injetar o servico no construtor (evita puxar ICustomerRepository/IEmailEventRepository/ITaskRepository/IUnitOfWork/ILogger<LeadScoringService> so para um metodo puro)"

key-files:
  modified:
    - api-core/src/Diax.Application/Customers/CustomerImportService.cs
    - api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs

key-decisions:
  - "Score calculado imediatamente apos customer.UpdateClassification(...) e antes de AddAsync — os 3 sinais novos da formula recalibrada (WebsiteKind, Quality, EmailType) e IsEligibleForCampaigns so existem no objeto depois dessas chamadas"
  - "Nenhuma dependencia nova injetada no construtor de CustomerImportService — LeadScoringService chamado como invocacao estatica, mesmo namespace (Diax.Application.Customers), sem using novo"
  - "Ramo ENRICH recebeu apenas um comentario de guarda (nao codigo), documentando por que o recalculo eh intencionalmente omitido ali — evita que alguem 'complete' a implementacao no futuro e destrua engajamento acumulado"

requirements-completed: [IMPT-03]

# Metrics
duration: ~15min
completed: 2026-09-07
---

# Phase 8 Plan 4: Lead Score no Momento do Import Summary

**Lead nasce com `LeadScore`/`Segment` preenchidos no ramo CREATE do import, usando `LeadScoringService.CalculateScore`/`SegmentForScore` do plano 08-02 (teto de fit 45); ramo ENRICH permanece intocado para não apagar engajamento acumulado.**

## Performance

- **Duration:** ~15 min
- **Completed:** 2026-09-07
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments

- `CustomerImportService.ImportAsync`, no ramo CREATE (lead novo), agora chama `LeadScoringService.CalculateScore(customer, null, DateTime.UtcNow)` e `customer.UpdateSegmentation(importScore, LeadScoringService.SegmentForScore(importScore))` logo após `UpdateClassification(...)` e antes de `AddAsync(...)` — o `Customer` sai persistido já com score e segmento.
- Ramo ENRICH (Customer já existente) recebeu um comentário de guarda explícito, sem nenhuma chamada de recálculo — protege o engajamento acumulado (aberturas, cliques, bounces) de um lead Hot contra ser rebaixado por um score recalculado com `engagement: null`.
- 3 testes novos em `CustomerImportServiceTests.cs`: lead forte nasce Warm (score 40), lead fraco nasce Cold (score 20), Customer Hot existente (score 75) permanece intocado no enriquecimento (com `AddAsync` verificado como nunca chamado nesse caso).
- Os valores aritméticos 40 e 20, derivados exatamente da tabela de pontos do plano 08-02, bateram na primeira tentativa — nenhuma investigação de causa-raiz foi necessária.
- Import e `LeadScoringWorker` (06:00 BRT) agora produzem sempre o mesmo score para o mesmo `Customer`, conforme exigido pela D-05.

## Task Commits

Each task was committed atomically (--no-verify):

1. **Task 1: Calcular score e segmento no ramo de criação do import** - `6fb3440` (feat)
2. **Task 2: Testes do score no import (Warm, Cold) e da não-recontagem no enriquecimento** - `c8fbc54` (test)

**Plan metadata:** (this commit) `docs(08-04): complete plan`

## Files Created/Modified

- `api-core/src/Diax.Application/Customers/CustomerImportService.cs` - chamada estática de `LeadScoringService.CalculateScore`/`SegmentForScore` no ramo CREATE após `UpdateClassification`; comentário de guarda no ramo ENRICH explicando por que o recálculo é intencionalmente omitido
- `api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs` - 3 testes novos (`Import_NewCustomer_GetsLeadScoreAndSegmentAtImport`, `Import_NewCustomer_WeakSignals_IsBornCold`, `Import_ExistingCustomer_LeadScoreNotRecomputedOnEnrich`)

## Decisions Made

- Ponto de inserção confirmado por leitura do arquivo real (não pelas linhas estáticas do plano, que já estavam desatualizadas pelo wave 2/plano 08-03) — âncora usada foi o conteúdo (`UpdateClassification(...)` → `AddAsync(...)`), como instruído pelo prompt do orquestrador.
- Nenhuma migration criada, nenhuma dependência nova injetada — `LeadScoringService` permanece com construtor de 5 dependências, chamado apenas como método estático puro pelo import.

## Deviations from Plan

### Nota sobre acceptance criteria conflitante (não é bug de implementação)

O `<action>` da Task 1 do plano especifica **verbatim** o comentário de guarda a ser inserido no ramo ENRICH, que contém a string `"IEmailEventRepository conhece"` (mencionando o tipo em prosa, para explicar por que o recálculo é perigoso ali). O bloco `<acceptance_criteria>` do mesmo plano, por outro lado, pede `grep -q "IEmailEventRepository" ... é FALSO`. Essas duas instruções são mutuamente contraditórias: seguir o texto exato do `<action>` (fonte de verdade sobre o que implementar) necessariamente faz o grep da acceptance criteria falhar, mesmo sem nenhuma injeção real de `IEmailEventRepository` ter sido adicionada (nenhum campo, nenhum parâmetro de construtor — confirmado por `grep -n "IEmailEventRepository"` retornando só a linha do comentário).

**Ação tomada:** implementei o comentário exatamente como o `<action>` especificou (é o texto autoritativo sobre o código a escrever), e documento aqui a divergência em vez de reescrever o comentário para "enganar" o grep ou de omitir a explicação que o próprio plano pediu. O objetivo real da acceptance criteria — nenhuma dependência nova injetada — foi verificado por outros meios (`grep -q "LeadScoringService _"` falso, ausência de novo parâmetro no construtor, `dotnet build` sem novo `using`).

Nenhum outro deviation. Todos os demais critérios de aceitação (grep de `CalculateScore`, `UpdateSegmentation` aparecendo 1×, ordem das linhas, `git diff --stat` vazio em `LeadScoringService.cs`, build e testes) passaram exatamente como especificado.

## Issues Encountered

None.

## Verificação Manual Pós-Deploy (registrar, não bloqueia o plano)

Conforme pedido no `<output>` do plano, a query abaixo deve ser executada após a primeira rodada do `ExtractorPullWorker` (12:00 BRT) seguinte ao deploy:

```sql
SELECT COUNT(*) FROM customers
WHERE source = 4 AND lead_score IS NULL AND created_at > '<data-do-deploy>'
```

Deve retornar 0 — todo Customer criado via Scraping após o deploy precisa nascer com `lead_score` preenchido.

## Efeito Esperado do Próximo LeadScoringWorker (06:00 BRT)

Este plano não altera `LeadScoringService.CalculateScore` (isso já foi feito no plano 08-02) — apenas passa a chamá-lo no import. O efeito de recalibração da base já existente (leads legados migrando de Cold para Warm por causa dos novos sinais de fit) já está documentado e aceito no `08-02-SUMMARY.md`. Nenhum efeito adicional é introduzido por este plano além do que 08-02 já previu.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- IMPT-03 está completo: leads importados nascem com `LeadScore`/`Segment` calculados, sem esperar o job diário.
- Phase 8 (Import — Dedup e Score em Tempo Real) está completa: IMPT-01, IMPT-02 (plano 08-03) e IMPT-03 (planos 08-02 + 08-04) todos entregues.
- Suíte completa verde: **891 testes passando, 0 falhando** (baseline 888 + 3 novos deste plano).
- Nenhum blocker.

---
*Phase: 08-import-—-dedup-e-score-em-tempo-real*
*Completed: 2026-09-07*

## Self-Check: PASSED

- FOUND: api-core/src/Diax.Application/Customers/CustomerImportService.cs
- FOUND: api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs
- FOUND commit: 6fb3440 (feat(08-04): calcula score e segmento no ramo CREATE do import (IMPT-03))
- FOUND commit: c8fbc54 (test(08-04): cobre score no import (Warm/Cold) e nao-recontagem no enrich)
