---
phase: 02-funda-o-de-agentes
plan: "01"
subsystem: agents-foundation
tags: [agents, domain, persistence, ef-core, migration, tdd]
dependency_graph:
  requires: []
  provides:
    - AgentPendingAction domain entity with IsPending/IsExpired invariants
    - IAgentPendingActionRepository contract
    - AgentPendingActionRepository EF Core implementation
    - AiConversation.AgentType nullable column
    - agent_pending_actions table in PROD DB
    - agent_type column on ai_conversations in PROD DB
    - AnthropicChatClient.CompleteWithToolsAsync (tool-use path, non-streaming)
  affects:
    - IAiChatRepository.GetPagedByUserAsync (new AgentType? filter param, backward compat)
    - AiChatService.ListConversationsAsync (call site fixed to use named param)
tech_stack:
  added: []
  patterns:
    - State-machine domain entity (AgentPendingAction.IsPending computed from nullability)
    - EF Core nullable column for type association (AgentType? on AiConversation)
    - Repository without SaveChanges (UoW handles commit)
    - Optional filter param with default null (backward-compatible IAiChatRepository change)
    - Anthropic tool-use opt-in extension (CompleteWithToolsAsync, non-breaking to streaming path)
key_files:
  created:
    - api-core/src/Diax.Domain/Agents/AgentPendingAction.cs
    - api-core/src/Diax.Domain/Agents/IAgentPendingActionRepository.cs
    - api-core/src/Diax.Infrastructure/Data/Repositories/AgentPendingActionRepository.cs
    - api-core/src/Diax.Infrastructure/Data/Migrations/20260529134701_AddAgentFoundation.cs
    - api-core/tests/Diax.Tests/Application/Agents/AgentPendingActionTests.cs
    - api-core/scripts/add-migration.ps1
  modified:
    - api-core/src/Diax.Domain/AiChat/AiConversation.cs
    - api-core/src/Diax.Domain/AiChat/IAiChatRepository.cs
    - api-core/src/Diax.Infrastructure/Data/Repositories/AiChatRepository.cs
    - api-core/src/Diax.Infrastructure/Data/DiaxDbContext.cs
    - api-core/src/Diax.Infrastructure/DependencyInjection.cs
    - api-core/src/Diax.Application/AiChat/AiChatService.cs
    - api-core/src/Diax.Infrastructure/AiChat/AnthropicChatClient.cs
decisions:
  - "Payload column uses nvarchar(max) (via HasColumnType fluent config) since it stores arbitrary JSON tool inputs"
  - "AgentType? nullable on AiConversation (no default value) — null=AiChat, non-null=agent conversation"
  - "GetPagedByUserAsync filter: null=no filter (existing callers unaffected), non-null=filter by type"
  - "CompleteWithToolsAsync added to AnthropicChatClient as opt-in method; StreamMessageAsync + CompleteAsync unchanged"
  - "add-migration.ps1 created (was missing from scripts/); dotnet-ef used with DOTNET_ROLL_FORWARD=LatestMajor on .NET 10 machine"
metrics:
  duration: "~35 minutes"
  completed_date: "2026-05-29"
  tasks_completed: 2
  tests_added: 7
  files_modified: 7
  files_created: 6
requirements_satisfied: [ORCH-04, ORCH-05]
---

# Phase 02 Plan 01: Agent Foundation Persistence Summary

**One-liner:** Nullable `AgentType` column on `AiConversation` + `AgentPendingAction` entity with state-machine invariants (IsPending/IsExpired) + EF Core migration `AddAgentFoundation` applied to PROD SQL Server.

## What Was Built

### Task 1: AgentPendingAction Domain Entity + Repository Contract + Tests (TDD)

`AgentPendingAction` is the source of truth for the confirmation flow (ORCH-04). A pending action row is created when an agent tool returns `PendingConfirmation`; the actual write happens only after `/confirm`.

**Public API:**

```csharp
public class AgentPendingAction
{
    // Ctor: validates toolName/toolUseId/payload not null/empty; sets ExpiresAt = UtcNow + ttlMinutes
    public AgentPendingAction(Guid userId, Guid conversationId, AgentType agentType,
        string toolName, string toolUseId, string actionLabel, string payload, int ttlMinutes = 15)

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsPending => ExecutedAt is null && CancelledAt is null && !IsExpired;

    public void MarkExecuted()   // Sets ExecutedAt → IsPending becomes false
    public void Cancel()         // Sets CancelledAt → IsPending becomes false
}
```

**Repository contract:**

```csharp
public interface IAgentPendingActionRepository
{
    Task AddAsync(AgentPendingAction action, CancellationToken ct = default);
    Task<AgentPendingAction?> GetPendingByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task UpdateAsync(AgentPendingAction action, CancellationToken ct = default);
}
```

**Tests:** 7 domain invariant tests covering all 4 behaviors specified + 3 validation cases.

### Task 2: AgentType on AiConversation + EF Repository + DbSet + Migration

**AiConversation change:**

```csharp
// New property (null = AiChat; non-null = agent conversation)
public AgentType? AgentType { get; private set; }

// New method
public void SetAgentType(AgentType agentType) { AgentType = agentType; SetUpdated(); }
```

**GetPagedByUserAsync new signature** (for Plans 03-05 to use):

```csharp
Task<PagedResult<AiConversation>> GetPagedByUserAsync(
    Guid userId, int page, int pageSize,
    bool includeArchived = false,
    AgentType? agentType = null,          // NEW — null = no filter (existing behavior preserved)
    CancellationToken cancellationToken = default);
```

**Migration applied:** `20260529134701_AddAgentFoundation`
- `agent_type` column on `ai_conversations`: `int`, `nullable: true`, no default value
- New table `agent_pending_actions` with all fields including `payload nvarchar(max)`

**AnthropicChatClient:** `CompleteWithToolsAsync` implemented (was defined in interface but missing impl). Returns `AnthropicToolResult.ToolUseRequest` when `stop_reason == "tool_use"`, or `TextResponse` for `end_turn`.

## Migration Details

| Migration | Applied | Details |
|-----------|---------|---------|
| `20260529134701_AddAgentFoundation` | YES — PROD | `agent_type nullable int` on `ai_conversations`; `agent_pending_actions` table |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Implementation] `AnthropicChatClient.CompleteWithToolsAsync` was missing**
- **Found during:** Task 1 test run (build error: class did not implement interface member)
- **Issue:** `IAnthropicChatClient.CompleteWithToolsAsync` was already defined in the interface (by a previous session) but `AnthropicChatClient` had no implementation
- **Fix:** Implemented `CompleteWithToolsAsync` + updated `BuildRequest`/`BuildRequestBody` to accept optional `IReadOnlyList<AnthropicToolDefinition>? tools` param + added tool serialization to request body
- **Files modified:** `api-core/src/Diax.Infrastructure/AiChat/AnthropicChatClient.cs`
- **Commit:** 2e57b6b

**2. [Rule 1 - Bug] Payload column defaulted to `nvarchar(256)` instead of `nvarchar(max)`**
- **Found during:** Task 2 migration generation review
- **Issue:** Global default max length of 256 for string properties caused `payload` to be `nvarchar(256)`, too small for JSON tool inputs
- **Fix:** Added `modelBuilder.Entity<AgentPendingAction>().Property(e => e.Payload).HasColumnType("nvarchar(max)")` in `DiaxDbContext.OnModelCreating`; removed and regenerated migration
- **Files modified:** `DiaxDbContext.cs`
- **Commit:** aaa751e

**3. [Rule 3 - Blocking] `add-migration.ps1` missing from scripts/**
- **Found during:** Task 2 — plan references this script but it didn't exist
- **Fix:** Created `api-core/scripts/add-migration.ps1`
- **Files created:** `api-core/scripts/add-migration.ps1`
- **Commit:** aaa751e

**4. [Rule 3 - Blocking] `dotnet ef` not runnable (requires .NET 8, only .NET 10 installed)**
- **Found during:** Task 2 migration generation
- **Issue:** `dotnet ef` shim invokes inner `ef.dll` with .NET 8 requirement; machine has only .NET 10
- **Fix:** Used `DOTNET_ROLL_FORWARD=LatestMajor dotnet-ef` (direct tool invocation) which picks up the `rollForward: Major` in the runtimeconfig and runs on .NET 10
- **Impact:** `update-db.ps1` has the same issue; will need same workaround in future phases

## Verification

- `dotnet test --filter "FullyQualifiedName~Agents"` → 11/11 passed (4 CommercialAgent + 7 AgentPendingAction)
- `dotnet build src/Diax.Api/Diax.Api.csproj` → 0 errors
- Migration `20260529134701_AddAgentFoundation` applied to PROD DB → `Done.`

## Known Stubs

None — this plan is purely persistence/domain layer with no UI or service stubs.

## Self-Check: PASSED

- [x] `api-core/src/Diax.Domain/Agents/AgentPendingAction.cs` exists
- [x] `api-core/src/Diax.Domain/Agents/IAgentPendingActionRepository.cs` exists
- [x] `api-core/src/Diax.Infrastructure/Data/Repositories/AgentPendingActionRepository.cs` exists
- [x] Migration `20260529134701_AddAgentFoundation.cs` exists
- [x] Commits `2e57b6b` and `aaa751e` exist
- [x] 11/11 tests pass
- [x] Build 0 errors
