---
phase: 08-import-—-dedup-e-score-em-tempo-real
plan: 03
subsystem: api
tags: [dedup, customer-import, compliance, extractor-integration]

# Dependency graph
requires:
  - phase: 08-import-—-dedup-e-score-em-tempo-real
    plan: 01
    provides: "ICustomerRepository.GetByExternalIdAsync + ImportCustomerRow.ExternalId + ExtractorIntegrationService.MapToImportRow propagando lead.Id"
provides:
  - "Resolucao de dedup ExternalId -> email -> telefone no loop de persistencia de CustomerImportService.ImportAsync, para TODAS as origens (Import/Scraping)"
  - "Guarda de compliance D-02: supressao do e-mail ANTIGO bloqueia troca de e-mail causada por match de ExternalId e propaga opt-out"
  - "Log greppavel ExternalIdConflict (D-03) quando ExternalId e e-mail casam com Customers diferentes"
  - "Backfill organico do ExternalId no ramo de enriquecimento (D-04)"
affects: [08-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "ILogger<T> injetado como ultimo parametro do construtor, mesmo padrao ja usado em LeadScoringService"
    - "Verificacao de ILogger.LogWarning via Moq: v.ToString()!.Contains(marcador) no It.Is<It.IsAnyType>, padrao canonico para metodos de extensao do ILogger"

key-files:
  modified:
    - api-core/src/Diax.Application/Customers/CustomerImportService.cs
    - api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs
    - api-core/tests/Diax.Tests/Customers/ExtractorIntegrationServiceTests.cs

key-decisions:
  - "Task 1 (resolucao de match) e Task 2 (guarda D-01/D-02) foram commitados juntos em um unico commit feat(08-03) porque as edicoes ficam fisicamente no mesmo bloco de codigo (linhas 404-470 do loop de persistencia) e nao ha diff limpo para separa-las sem reconstruir hunks manualmente. O plano ja reconhece essa proximidade (IMPT-01/IMPT-02 sao a mesma mudanca vista de dois angulos); a mesma logica se aplica aqui dentro do proprio plano 08-03."
  - "Task 3 (os 6 testes) ficou em commit separado test(08-03), preservando a granularidade tarefa-por-commit onde o arquivo era distinto"
  - "No caso de conflito D-03, o backfill do ExternalId (D-04) e explicitamente pulado no Customer que NAO recebeu o match direto por ExternalId, coberto pelo teste Import_ExternalIdAndEmailMatchDifferentCustomers_EmailWins_LogsConflict"

requirements-completed: [IMPT-01, IMPT-02]

duration: ~25min
completed: 2026-09-07
---

# Phase 8 Plan 3: Dedup do Import por ExternalId com Guardas D-01..D-04 Summary

**Resolução de match `ExternalId → e-mail → telefone` no loop de persistência de `CustomerImportService`, com troca de e-mail governada pelo Extrator (D-01), guarda de compliance contra ressuscitar contato bloqueado via e-mail antigo suprimido (D-02), e-mail vencendo em conflito de identidade com log greppável (D-03) e backfill orgânico do `ExternalId` (D-04).**

## Performance

- **Duration:** ~25 min
- **Completed:** 2026-09-07
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments

- `CustomerImportService` agora tenta `GetByExternalIdAsync` ANTES de `GetByEmailAsync`/`GetByPhoneAsync` no loop de persistência, para todas as origens (`Import` e `Scraping` compartilham o mesmo caminho de match/enriquecimento — apenas o bloco de validação hard das linhas 86-313, intocado, diverge entre elas).
- D-01: quando o `ExternalId` casa e o e-mail da linha é diferente do e-mail do Customer, o e-mail é atualizado in-place via `UpdateBasicInfo`, preservando `EmailOptOut`, Notes, Tags, timeline e `CreatedAt` (nenhum Customer é recriado — único `new Customer(` do arquivo continua sendo o do ramo CREATE).
- D-02: duas camadas de defesa contra ressuscitar um contato bloqueado.
  1. A checagem pré-existente `existingCustomer.EmailOptOut` (linhas ~443-450) continua intocada e barra a linha antes de qualquer troca.
  2. Camada nova: quando há troca de e-mail por match de `ExternalId`, o e-mail ANTIGO do Customer é checado contra `IEmailSuppressionRepository.IsSuppressedAsync` — se suprimido, `OptOutEmail()` é chamado no Customer (propagando o bloqueio para a próxima passada), `UpdateAsync` persiste a flag, e a linha é rejeitada sem trocar o e-mail.
- D-03: quando o `ExternalId` casa com um Customer A e o e-mail casa com um Customer B diferente, o e-mail vence — B é enriquecido, A permanece intocado (nenhum backfill de `ExternalId` nele), e um `_logger.LogWarning` com o marcador literal `ExternalIdConflict:` registra os dois IDs envolvidos.
- D-04: quando o match veio por e-mail/telefone (não por `ExternalId`) e a linha trouxe um `ExternalId`, ele é gravado no Customer existente se ainda estiver vazio — backfill orgânico sem migration nem script de mutação em massa.
- `ILogger<CustomerImportService>` injetado (necessário para o log de conflito D-03); os dois pontos de construção do serviço em teste (`CustomerImportServiceTests.cs`, `ExtractorIntegrationServiceTests.cs`) foram atualizados.
- 6 testes novos cobrindo D-01, D-02 (flag própria + supressão órfã), D-03, D-04 e o double-pull de Scraping com troca de e-mail entre passadas.

## Task Commits

Each task was committed atomically (Task 1 e Task 2 combinados — ver Decisions):

1. **Task 1 + Task 2: Resolução de match ExternalId → e-mail → telefone + guarda D-01/D-02** - `ccc1a4d` (feat)
2. **Task 3: Os 6 testes de dedup por ExternalId** - `f9bb51b` (test)

**Plan metadata:** (this commit) `docs(08-03): complete plan`

## Files Created/Modified

- `api-core/src/Diax.Application/Customers/CustomerImportService.cs` - resolução de match ExternalId → e-mail → telefone, guarda de compliance D-02, log D-03, backfill D-04, `ILogger<CustomerImportService>` injetado
- `api-core/tests/Diax.Tests/Customers/CustomerImportServiceTests.cs` - 6 testes novos (`Import_ExternalIdMatch_UpdatesEmail_WhenEmailChanged`, `Import_ExternalIdMatch_PreservesEmailOptOut_WhenEmailChanged`, `Import_ExternalIdMatch_PreservesEmailOptOut_WhenOldEmailIsSuppressed`, `Import_ExternalIdAndEmailMatchDifferentCustomers_EmailWins_LogsConflict`, `Import_EmailMatchWithExternalId_BackfillsExternalIdOnExistingCustomer`, `Import_Scraping_DoublePull_SameExternalId_DifferentEmail_NoDuplicateCreated`), helper `ExistingCustomer`, `_loggerMock`
- `api-core/tests/Diax.Tests/Customers/ExtractorIntegrationServiceTests.cs` - construtor do SUT atualizado com `Mock.Of<ILogger<CustomerImportService>>()`

## Decisions Made

- Task 1 e Task 2 commitadas juntas (`ccc1a4d`) — ambas editam o mesmo bloco de linhas do loop de persistência (404-470) e não há forma limpa de separar os hunks sem reconstruir diffs manualmente. Documentado como deviation de execução (não de escopo): ambas as tasks foram implementadas e verificadas conforme o plano, só o agrupamento de commit mudou.
- Nenhuma migration criada, nenhum backfill em massa escrito — ambos explicitamente fora de escopo por D-04/CONTEXT.md e verificados pelo `git status --porcelain` vazio em `Migrations/`.
- O conflito D-03 nunca reescreve o `ExternalId` do Customer que "perdeu" o conflito — condição `!externalIdConflict` no bloco de backfill garante isso, coberto pelo teste dedicado.

## Deviations from Plan

### Auto-fixed Issues

Nenhuma. O plano já vinha com o diff completo (blocos de código prontos no `<action>` de cada task) e todas as âncoras de linha batiam com o arquivo real após wave 1/08-02 — nenhuma correção de rota necessária.

### Execução

**1. Task 1 e Task 2 commitadas em um único commit** — ambas tocam o mesmo bloco físico de `CustomerImportService.cs` (resolução de match + guarda de compliance ficam entrelaçadas nas mesmas ~60 linhas). Separar exigiria reconstruir os hunks manualmente sem ganho real de rastreabilidade (o próprio plano já descreve as duas tasks como uma extensão direta uma da outra). Commit único `ccc1a4d` cobre as duas; Task 3 (arquivo de teste distinto) manteve commit próprio `f9bb51b`.

## Auth Gates

Nenhum.

## Known Stubs

Nenhum. Toda a lógica implementada é exercitada pelos 6 testes novos mais os 23 pré-existentes de `CustomerImportServiceTests` (29 no total) e os 61 de `ExtractorIntegrationServiceTests`.

## Deferred Ideas Noted (per plano)

- **Backfill em massa do `external_id` dos ~2300 Customers já importados** continua FORA de escopo. D-04 (backfill orgânico) resolve organicamente conforme os pulls diários do `ExtractorPullWorker` rodam. Reavaliar apenas se o ritmo orgânico se mostrar insuficiente (ver `.planning/phases/08-import-—-dedup-e-score-em-tempo-real/08-CONTEXT.md`, seção Deferred).
- **Discrepância informativa não resolvida:** o código grava o marcador `"ID Extrator:"` em `Notes` desde abril, mas uma consulta de produção reportou 0 linhas com esse marcador. Não investigado nesta execução (fora do escopo do plano) — anotado para verificação futura via `SELECT` read-only se a conversa de backfill em massa voltar à mesa.

## User Setup Required

Nenhum — nenhuma configuração de serviço externo necessária.

## Next Phase Readiness

- A resolução de dedup por `ExternalId` está completa e testada; `CustomerImportService.ImportAsync` agora resolve corretamente leads que trocam de e-mail entre passadas do Extrator, sem nunca recriar Customer nem burlar opt-out/supressão.
- `LeadScoringService.SegmentForScore(int)` (de 08-02) e o `ExternalId` gravado no import (deste plano) ficam disponíveis para o plano 08-04 (score em tempo real no import).
- Suíte completa verde: **888 testes passando, 0 falhando** (baseline 882 + 6 novos deste plano).
- Nenhum blocker.

---
*Phase: 08-import-—-dedup-e-score-em-tempo-real*
*Completed: 2026-09-07*

## Self-Check: PASSED

Arquivos modificados verificados em disco; commits `ccc1a4d` e `f9bb51b` verificados presentes em `git log`.
