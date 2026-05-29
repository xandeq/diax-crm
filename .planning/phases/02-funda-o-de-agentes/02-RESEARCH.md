# Phase 2: Fundação de Agentes — Research

**Researched:** 2026-05-28
**Domain:** .NET 8 Clean Architecture — Anthropic tool-use, agent orchestration, pending-confirmation pattern
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- Motor de chat ÚNICO reaproveitando `IAnthropicChatClient` / `AiChatService`. NÃO criar novo provider LLM.
- O endpoint já existente `POST /api/v1/agents/commercial/chat` (CommercialAgentService) NÃO pode quebrar. O orquestrador deve acomodá-lo ou o Commercial deve passar a rotear por ele sem mudança de contrato externo.
- `AgentType` enum já existe (Personal=0, Support=1, Commercial=2) em `Diax.Domain/Agents`.
- Persistência reusa `AiConversation` (entidade/-repos de AiChat), associando o tipo de agente à conversa.
- RBAC reusa `GroupAiAccess` (validação já usada em BaseAiController/AiCatalog). Uso reusa `AiUsageTracking`.
- Padrões a seguir: Result pattern + `HandleResult`, `BaseApiController`/`GetCurrentUserId()`, services em `Diax.Application/<Feature>` com interface, DTOs em subpasta, registro em `Diax.Application/DependencyInjection.cs`, testes Moq+xUnit.

### Claude's Discretion
- Forma exata de modelar a "ação pendente" (tabela nova vs. estado em memória/conversa vs. token assinado)
- Assinatura concreta das interfaces `IAgentOrchestrator`, `IAgentTool`, `IAgentHandler`
- Como associar `AgentType` à `AiConversation` (coluna nova vs. metadado)

### Deferred Ideas (OUT OF SCOPE)
- Tools específicas de cada agente (Comercial/Suporte/Pessoal) — fases 3/4/5
- UI — Phase 6
- Streaming com tool-use no frontend — Phase 6 (esta fase pode entregar non-streaming para tool-use se simplificar)
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ORCH-01 | AgentOrchestrator roteia a conversa para o handler do tipo de agente correto (Comercial/Suporte/Pessoal) | IAgentOrchestrator + IAgentHandler pattern; AgentType enum already exists |
| ORCH-02 | Cada agente compartilha o mesmo motor de chat, variando apenas system prompt, ferramentas e escopo de dados | IAnthropicChatClient reuse; new IAnthropicToolClient sibling; AnthropicMessageRequest extended opt-in |
| ORCH-03 | Acesso aos agentes respeita RBAC/grupos (reuso GroupAiAccess) e o uso é registrado (reuso AiUsageTracking) | BaseAiController.ExecuteAiActionAsync already does RBAC; IAiUsageTrackingService.LogUsageAsync for featureType="agent:{type}" |
| ORCH-04 | Framework de tools reutilizável; toda tool que grava dados retorna ação pendente que exige confirmação | IAgentTool interface; PendingAction row in DB (lightweight migration); /confirm endpoint |
| ORCH-05 | Conversas dos agentes são persistidas e retomáveis; tipo de agente associado à AiConversation | AgentType? nullable column on ai_conversations — single lightweight migration |
</phase_requirements>

---

## Summary

Phase 2 builds the orchestration backbone that Phases 3–5 plug into. The two hardest problems are: (1) extending the raw HttpClient-based `AnthropicChatClient` to support tool-use without touching the existing streaming path, and (2) designing the confirmation flow for write actions. Both are tractable with the patterns below.

The Anthropic Messages API tool-use contract is well-defined and stable (API version `2023-06-01`). Tool-use calls are **non-streaming by default** — when `stop_reason == "tool_use"` the response is a complete JSON object, not an SSE stream. This is a strong reason to keep the tool-use path non-streaming in Phase 2, consistent with the locked decision to defer streaming tool-use to Phase 6.

For the confirmation flow, a **single `agent_pending_actions` table** is the recommended approach: lightweight (one migration), stateless-friendly (no in-memory state between requests), signed-token alternatives add crypto complexity with no real benefit given the single-user system.

**Primary recommendation:** Add a second method `CompleteWithToolsAsync` to `IAnthropicChatClient` (opt-in, non-breaking), build `IAgentTool` / `IAgentHandler` / `IAgentOrchestrator` interfaces, associate `AgentType` via nullable column on `ai_conversations`, and store pending actions in `agent_pending_actions`.

---

## Standard Stack

### Core (already in the project — reuse only)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Diax.Application.AiChat.IAnthropicChatClient | project | LLM gateway | Locked decision — do not replace |
| Diax.Domain.AiChat.AiConversation | project | Conversation persistence | Locked decision — reuse |
| Diax.Application.AI.GroupAiAccessService | project | RBAC | Locked decision — reuse |
| Diax.Application.AI.Services.AiUsageTrackingService | project | Usage logging | Locked decision — reuse |
| xUnit + Moq + FluentAssertions | project test suite | Testing | Existing convention |

### New additions (Phase 2)
| Library | Version | Purpose | Notes |
|---------|---------|---------|-------|
| System.Text.Json | .NET 8 built-in | Serialize tool input_schema and tool_result payloads | Already used in AnthropicChatClient.cs |
| Microsoft.AspNetCore.DataProtection | .NET 8 built-in | Optional: sign pending-action tokens | Only if token approach is chosen; not recommended |

**No new NuGet packages required** — everything is achievable with what is already in the project.

---

## Architecture Patterns

### Recommended Project Structure (new files only)

```
Diax.Domain/
└── Agents/
    ├── AgentType.cs                  (EXISTS — do not change)
    ├── AgentPendingAction.cs         (NEW — domain entity)
    └── IAgentPendingActionRepository.cs (NEW)

Diax.Application/
└── Agents/
    ├── Commercial/                   (EXISTS — do not break)
    ├── IAgentTool.cs                 (NEW — tool contract)
    ├── IAgentHandler.cs              (NEW — per-agent handler contract)
    ├── IAgentOrchestratorService.cs  (NEW — routing interface)
    ├── AgentOrchestratorService.cs   (NEW — routes by AgentType)
    ├── Dtos/
    │   ├── AgentChatRequest.cs       (NEW)
    │   ├── AgentChatResponse.cs      (NEW)
    │   ├── AgentConfirmRequest.cs    (NEW)
    │   └── PendingActionDto.cs       (NEW)
    └── Commercial/                   (EXISTS — CommercialAgentService adapts to IAgentHandler)

Diax.Infrastructure/
├── AiChat/
│   └── AnthropicChatClient.cs        (EXISTS — add CompleteWithToolsAsync, non-breaking)
└── Data/
    ├── Repositories/
    │   └── AgentPendingActionRepository.cs (NEW)
    └── Migrations/
        ├── AddAgentTypeToConversations.cs  (NEW — nullable AgentType column)
        └── AddAgentPendingActions.cs       (NEW — pending actions table)

Diax.Api/
└── Controllers/V1/
    └── AgentsController.cs           (EXISTS — extend with new endpoints, keep /commercial/chat)
```

### Pattern 1: IAgentTool — The Tool Contract

Every tool Phase 3/4/5 registers implements this interface. Read-only tools execute directly; write tools return a `PendingAction`.

```csharp
// Diax.Application/Agents/IAgentTool.cs
public interface IAgentTool
{
    /// <summary>Must match regex ^[a-zA-Z0-9_-]{1,64}$</summary>
    string Name { get; }
    string Description { get; }
    /// <summary>JSON Schema object — serialized to JsonElement for the Anthropic request.</summary>
    JsonElement InputSchema { get; }
    bool RequiresConfirmation { get; }  // true = write action
    /// <summary>
    /// Execute the tool. Read-only tools: return AgentToolResult.Ok(data).
    /// Write tools: return AgentToolResult.PendingConfirmation(label, payload).
    /// NEVER execute writes here — always gate behind RequiresConfirmation.
    /// </summary>
    Task<AgentToolResult> ExecuteAsync(
        JsonElement input,
        Guid userId,
        CancellationToken ct = default);
}

public abstract record AgentToolResult
{
    public record Ok(string Content) : AgentToolResult;
    public record PendingConfirmation(string ActionLabel, string SerializedPayload) : AgentToolResult;
    public record Failure(string Error) : AgentToolResult;
}
```

### Pattern 2: IAgentHandler — Per-Agent Behavior

Each agent (Commercial, Support, Personal) implements one handler. The handler owns the system prompt and declares its tools. Phase 2 includes a placeholder `CommercialAgentHandler` that wraps the existing `CommercialAgentService` without breaking its endpoint.

```csharp
// Diax.Application/Agents/IAgentHandler.cs
public interface IAgentHandler
{
    AgentType AgentType { get; }
    string BuildSystemPrompt(Guid userId, object? context);
    IReadOnlyList<IAgentTool> GetTools();  // empty list in Phase 2; filled in Phase 3/4/5
}
```

### Pattern 3: Extending AnthropicChatClient (OPT-IN, Non-Breaking)

The current `IAnthropicChatClient` has two methods. Add a third **sibling method** that accepts tools and returns a non-streaming result. The existing `StreamMessageAsync` and `CompleteAsync` are untouched.

**Approach:** Extend `AnthropicMessageRequest` with optional tools fields and add `CompleteWithToolsAsync` to the interface. The `BuildRequestBody` method in `AnthropicChatClient` checks for the presence of tools and serializes accordingly. When `tools` is null or empty, the body is identical to today — no behavior change.

```csharp
// Extension to IAnthropicChatClient.cs (add to existing file)
public interface IAnthropicChatClient
{
    // EXISTING — do not change signatures:
    IAsyncEnumerable<AnthropicStreamEvent> StreamMessageAsync(
        AnthropicMessageRequest request, CancellationToken cancellationToken = default);
    Task<AnthropicCompletionResult> CompleteAsync(
        AnthropicMessageRequest request, CancellationToken cancellationToken = default);

    // NEW — opt-in tool-use path (non-streaming):
    Task<AnthropicToolResult> CompleteWithToolsAsync(
        AnthropicMessageRequest request,
        IReadOnlyList<AnthropicToolDefinition> tools,
        CancellationToken cancellationToken = default);
}

// New types (add to IAnthropicChatClient.cs)
public record AnthropicToolDefinition(
    string Name,
    string Description,
    JsonElement InputSchema);  // pre-serialized JSON Schema object

public abstract record AnthropicToolResult
{
    /// <summary>Model responded with text (stop_reason = "end_turn").</summary>
    public record TextResponse(string Text, AnthropicUsage Usage) : AnthropicToolResult;
    /// <summary>Model wants to call a tool (stop_reason = "tool_use").</summary>
    public record ToolUseRequest(
        string ToolUseId,   // id field from tool_use block
        string ToolName,
        JsonElement ToolInput,
        AnthropicUsage Usage) : AnthropicToolResult;
}
```

**Implementation note for `AnthropicChatClient.CompleteWithToolsAsync`:** The request body JSON gains a `"tools"` array (each entry has `name`, `description`, `input_schema`) and the response is parsed for `stop_reason`. When `stop_reason == "tool_use"`, find the `tool_use` block in `content[]` and return `ToolUseRequest`. The tool_result continuation (second API call) is handled by the orchestrator, not the client.

### Pattern 4: Anthropic Tool-Use API — Exact Wire Format

**Source:** Official Anthropic docs (verified 2026-05-28), API version `2023-06-01`.

**Request body with tools:**
```json
{
  "model": "claude-sonnet-4-5",
  "max_tokens": 2048,
  "tools": [
    {
      "name": "update_lead_status",
      "description": "Updates the status of a lead in the CRM...",
      "input_schema": {
        "type": "object",
        "properties": {
          "lead_id": { "type": "string", "description": "UUID of the lead" },
          "new_status": { "type": "string", "enum": ["Contacted","Qualified","Negotiating","Customer"] }
        },
        "required": ["lead_id", "new_status"]
      }
    }
  ],
  "messages": [
    { "role": "user", "content": [{ "type": "text", "text": "Atualiza o lead X para Qualified" }] }
  ]
}
```

**Response when model calls a tool (`stop_reason = "tool_use"`):**
```json
{
  "id": "msg_01Aq9w938a90dw8q",
  "stop_reason": "tool_use",
  "content": [
    { "type": "text", "text": "Vou atualizar o status do lead para você." },
    {
      "type": "tool_use",
      "id": "toolu_01A09q90qw90lq917835lq9",
      "name": "update_lead_status",
      "input": { "lead_id": "abc-123", "new_status": "Qualified" }
    }
  ],
  "usage": { "input_tokens": 430, "output_tokens": 72 }
}
```

**Tool result continuation (second request — after confirmation):**
```json
{
  "role": "user",
  "content": [
    {
      "type": "tool_result",
      "tool_use_id": "toolu_01A09q90qw90lq917835lq9",
      "content": "Lead atualizado para Qualified com sucesso."
    }
  ]
}
```

**Critical constraints (verified):**
- `tool_result` blocks must be **first** in the user content array; any text must come after.
- Tool result message must immediately follow the tool_use message in the conversation history.
- `stop_reason = "tool_use"` means non-streaming response is complete — the full JSON is available immediately.
- Tool names must match `^[a-zA-Z0-9_-]{1,64}$`.
- `input_schema` is a JSON Schema `object` type — serialize from C# using `System.Text.Json`.

### Pattern 5: Confirmation Flow — Recommended Approach (DB Row)

**Decision (discretion area):** Use a **`agent_pending_actions` table** rather than signed tokens or in-memory state.

**Reasoning:**
- In-memory: fails on app restart / multiple replicas — not suitable for production.
- Signed token (HMAC): adds complexity (key management, token size in response), offers no benefit in a single-user system.
- DB row: one migration, aligns with existing EF Core patterns, survives restarts, auditable.

**Schema (`agent_pending_actions`):**
```sql
CREATE TABLE agent_pending_actions (
    id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID() PRIMARY KEY,
    user_id         UNIQUEIDENTIFIER NOT NULL,
    conversation_id UNIQUEIDENTIFIER NOT NULL,  -- FK to ai_conversations
    agent_type      INT NOT NULL,               -- AgentType enum
    tool_name       NVARCHAR(64) NOT NULL,
    tool_use_id     NVARCHAR(128) NOT NULL,     -- Anthropic's toolu_xxx id
    action_label    NVARCHAR(256) NOT NULL,     -- human-readable label for UI button
    payload         NVARCHAR(MAX) NOT NULL,     -- JSON: serialized tool input
    expires_at      DATETIME2 NOT NULL,         -- e.g., NOW + 15 minutes
    created_at      DATETIME2 NOT NULL,
    executed_at     DATETIME2 NULL,             -- NULL = pending, NOT NULL = completed
    cancelled_at    DATETIME2 NULL
);
```

**Flow:**
1. `POST /api/v1/agents/{type}/chat` — orchestrator calls `CompleteWithToolsAsync`; if `ToolUseRequest` returned, inserts `AgentPendingAction` row, returns `PendingActionDto` to frontend (action_label + pending_action_id).
2. Frontend shows confirmation button ("Atualizar status para Qualified — Confirmar?").
3. `POST /api/v1/agents/{type}/confirm` — reads pending action row, executes the actual write via the tool's `ExecuteConfirmedAsync` method, marks row executed, calls Anthropic again with `tool_result` to get the final text reply.

**Domain entity:**
```csharp
// Diax.Domain/Agents/AgentPendingAction.cs
public class AgentPendingAction
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid ConversationId { get; private set; }
    public AgentType AgentType { get; private set; }
    public string ToolName { get; private set; }
    public string ToolUseId { get; private set; }     // Anthropic's toolu_xxx
    public string ActionLabel { get; private set; }
    public string Payload { get; private set; }       // JSON
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExecutedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsPending => ExecutedAt is null && CancelledAt is null && !IsExpired;

    public void MarkExecuted() { ExecutedAt = DateTime.UtcNow; }
    public void Cancel() { CancelledAt = DateTime.UtcNow; }
}
```

### Pattern 6: AgentType Association with AiConversation

**Decision (discretion area):** Add a **nullable `AgentType?` column** to `ai_conversations`.

**Reasoning:**
- Null = AiChat (existing conversations, no change).
- Non-null = Agent conversation of the given type.
- Single lightweight migration, no data change to existing rows.
- Avoids a separate table for agent conversations.
- The `IAiChatRepository` `GetPagedByUserAsync` gains an optional `AgentType?` filter parameter (default null returns all, matching existing behavior).

**Migration (EF Core, applied via `add-migration.ps1` + `update-db.ps1`):**
```csharp
migrationBuilder.AddColumn<int>(
    name: "agent_type",
    table: "ai_conversations",
    type: "int",
    nullable: true,
    defaultValue: null);
```

**AiConversation domain changes (minimal):**
```csharp
// Add to AiConversation.cs
public AgentType? AgentType { get; private set; }

public void SetAgentType(AgentType agentType)
{
    AgentType = agentType;
    SetUpdated();
}
```

### Pattern 7: Streaming vs. Non-Streaming Decision

**Tool-use path (Phase 2):** Use `CompleteWithToolsAsync` (non-streaming). Rationale:
- When `stop_reason == "tool_use"`, streaming would require buffering the entire response anyway to detect and parse `tool_use` content blocks.
- The Anthropic API sends `tool_use` blocks only when the response is complete — streaming gives no latency benefit here.
- Non-streaming is simpler, eliminates SSE parsing for tool turns, and is consistent with the existing `CommercialAgentService.ChatAsync` pattern.
- If the agent responds with text only (`stop_reason == "end_turn"`), the non-streaming call is fast.

**Text-only path (existing AiChat):** Keeps streaming via `StreamMessageAsync` — unchanged.

**Phase 6:** When UI is built, the final text response after tool confirmation can stream; the tool negotiation itself stays non-streaming.

### Pattern 8: Orchestrator Routing

```csharp
// Diax.Application/Agents/AgentOrchestratorService.cs
public class AgentOrchestratorService : IAgentOrchestratorService
{
    private readonly IReadOnlyDictionary<AgentType, IAgentHandler> _handlers;
    private readonly IAnthropicChatClient _anthropic;
    private readonly IAiChatRepository _conversationRepo;
    private readonly IAgentPendingActionRepository _pendingRepo;
    private readonly IUnitOfWork _uow;

    // Constructor registers handlers by type — injected via IEnumerable<IAgentHandler>
    public AgentOrchestratorService(
        IEnumerable<IAgentHandler> handlers,
        IAnthropicChatClient anthropic,
        IAiChatRepository conversationRepo,
        IAgentPendingActionRepository pendingRepo,
        IUnitOfWork uow)
    {
        _handlers = handlers.ToDictionary(h => h.AgentType);
        // ...
    }

    public async Task<Result<AgentChatResponse>> ChatAsync(
        AgentType agentType, Guid userId, AgentChatRequest request, CancellationToken ct)
    {
        if (!_handlers.TryGetValue(agentType, out var handler))
            return Result.Failure<AgentChatResponse>(new Error("NotFound", "Tipo de agente não suportado."));

        var conv = await LoadOrCreateConversationAsync(userId, agentType, request.ConversationId, ct);
        var tools = handler.GetTools();
        var system = handler.BuildSystemPrompt(userId, request.Context);

        if (tools.Count == 0)
        {
            // Text-only path — reuse CompleteAsync (CommercialAgent Phase 2 behavior)
            var result = await _anthropic.CompleteAsync(BuildRequest(conv, request.Message, system), ct);
            await PersistMessagesAsync(conv, request.Message, result.Text, result.Usage, ct);
            return Result<AgentChatResponse>.Success(new AgentChatResponse(
                ConversationId: conv.Id,
                Reply: result.Text,
                PendingAction: null,
                Usage: MapUsage(result.Usage)));
        }
        else
        {
            // Tool-use path
            var toolDefs = tools.Select(t => new AnthropicToolDefinition(t.Name, t.Description, t.InputSchema)).ToList();
            var toolResult = await _anthropic.CompleteWithToolsAsync(BuildRequest(conv, request.Message, system), toolDefs, ct);

            return toolResult switch
            {
                AnthropicToolResult.TextResponse text => /* persist + return */,
                AnthropicToolResult.ToolUseRequest toolUse => await HandleToolUseAsync(conv, toolUse, tools, userId, ct),
                _ => Result.Failure<AgentChatResponse>(new Error("Agent.Unknown", "Resultado inesperado do modelo."))
            };
        }
    }
}
```

### Pattern 9: RBAC Reuse

The existing `AgentsController` does NOT extend `BaseAiController` — it uses bare `BaseApiController`. For Phase 2, the new generic endpoints (`POST /agents/{type}/chat`, `POST /agents/{type}/confirm`) should either:

- **Option A (recommended):** Keep `AgentsController` extending `BaseApiController` and inject `IAiCatalogService` directly for the RBAC check (same logic as `BaseAiController.ExecuteAiActionAsync`). This avoids refactoring the existing controller.
- **Option B:** Migrate to `BaseAiController`. Requires adding constructor params — possible but riskier for the existing `commercial/chat` endpoint.

For `featureType` in `AiUsageTrackingService.LogUsageAsync`, use `"agent:commercial"`, `"agent:support"`, `"agent:personal"`.

### Anti-Patterns to Avoid

- **Writing data before confirmation:** The `ExecuteAsync` method on `IAgentTool` must NEVER write data when `RequiresConfirmation == true`. Only `ExecuteConfirmedAsync` (called after `/confirm`) may write. Unit tests must verify this.
- **Breaking CommercialAgentService:** The existing `POST /agents/commercial/chat` endpoint returns `CommercialAgentResponse` — do not change this DTO or route. The new generic `POST /agents/{type}/chat` is a separate endpoint.
- **Streaming in tool-use path:** Do not attempt to use `StreamMessageAsync` for tool-calling turns. Buffer the full response via `CompleteWithToolsAsync`.
- **In-memory pending actions:** Never store `PendingAction` in a static dictionary or scoped service. App pools recycle; the DB row is the source of truth.
- **Storing full conversation history in pending action:** Store only `tool_use_id` and `payload`. The conversation context is in `ai_conversations` / `ai_chat_messages`.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| JSON Schema for tool input_schema | Custom schema builder | Serialize from `JsonElement` or anonymous objects using `System.Text.Json` | JSON Schema is just a JSON object — no special library needed |
| HMAC-signed confirmation tokens | Custom token signing | DB row in `agent_pending_actions` | Simpler, auditable, survives restarts, consistent with project patterns |
| Tool discovery / registration | Plugin system | DI: `IEnumerable<IAgentHandler>` injected into orchestrator | .NET DI handles this natively — register handlers in DI, orchestrator receives all |
| Anthropic SDK | NuGet `Anthropic` SDK | Extend existing `AnthropicChatClient.CompleteWithToolsAsync` | Project already has a raw HttpClient implementation; SDK would be a breaking change |

---

## Common Pitfalls

### Pitfall 1: tool_result ordering constraint
**What goes wrong:** Sending a `tool_result` in the user message with text content placed BEFORE the tool_result block causes HTTP 400 from Anthropic.
**Why it happens:** Anthropic's API enforces that `tool_result` blocks must come first in the user content array.
**How to avoid:** In `BuildToolResultMessage`, always put `tool_result` blocks first; any additional text content appended after.
**Warning signs:** HTTP 400 with "tool_use ids were found without tool_result blocks immediately after".

### Pitfall 2: content array vs. string content
**What goes wrong:** The existing `AnthropicChatClient.BuildRequestBody` sends messages as `content: [{"type":"text","text":"..."}]` (array form). When tool_use responses come back, the `content` in the assistant message is also an array — including `tool_use` blocks. If you persist the full response as a string, you lose the `tool_use_id` needed for the `/confirm` step.
**How to avoid:** Persist `tool_use_id` in the `AgentPendingAction` row immediately when the tool_use response arrives. Do not rely on reconstructing it from conversation history.

### Pitfall 3: AgentType nullable vs. zero default
**What goes wrong:** If `AgentType` column defaults to `0` (Personal) instead of `NULL`, existing AiChat conversations appear as "Personal agent" conversations.
**How to avoid:** Set `nullable: true` with no default value in the migration. Access pattern: `conv.AgentType == null` means AiChat; non-null means agent conversation.

### Pitfall 4: CommercialAgentService not adapted to IAgentHandler
**What goes wrong:** Phase 2 builds the orchestrator but CommercialAgentService remains as-is with no `IAgentHandler` implementation, so Phase 3 (which should build Commercial agent tools) has no handler to plug into.
**How to avoid:** Phase 2 creates a minimal `CommercialAgentHandler : IAgentHandler` that wraps `CommercialAgentService` for now (with `GetTools()` returning empty list) and registers it in DI. Phase 3 enriches it.

### Pitfall 5: PendingAction expiry not checked
**What goes wrong:** A stale pending action is confirmed hours/days later, executing a write on outdated data.
**How to avoid:** `/confirm` endpoint checks `IsExpired` before executing. Recommend 15-minute TTL. Optionally add a background cleanup (low priority — table stays small in single-user system).

### Pitfall 6: Missing AiConversation query filter for agent conversations
**What goes wrong:** `GetPagedByUserAsync` in `IAiChatRepository` returns AiChat conversations mixed with agent conversations in the `/ai-chat` UI.
**How to avoid:** Add optional `AgentType? agentType = null` parameter to `GetPagedByUserAsync`. Null = no filter (existing behavior). Pass `agentType: null` from `AiChatService` (unchanged); pass explicit value from agent orchestrator.

---

## Code Examples

### Extending BuildRequestBody in AnthropicChatClient (tool support)

```csharp
// Source: AnthropicChatClient.cs — extension to BuildRequestBody
private static string BuildRequestBody(
    AnthropicMessageRequest req,
    bool stream,
    IReadOnlyList<AnthropicToolDefinition>? tools = null)
{
    var root = new JsonObject { /* ... existing ... */ };

    // Tool definitions — only added when tools are provided
    if (tools is { Count: > 0 })
    {
        var toolsArray = new JsonArray();
        foreach (var t in tools)
        {
            toolsArray.Add(new JsonObject
            {
                ["name"] = t.Name,
                ["description"] = t.Description,
                ["input_schema"] = JsonNode.Parse(t.InputSchema.GetRawText())
            });
        }
        root["tools"] = toolsArray;
    }

    // ... existing messages serialization unchanged ...
    return root.ToJsonString();
}
```

### Parsing tool_use response in CompleteWithToolsAsync

```csharp
// Source: AnthropicChatClient.cs — new method
public async Task<AnthropicToolResult> CompleteWithToolsAsync(
    AnthropicMessageRequest request,
    IReadOnlyList<AnthropicToolDefinition> tools,
    CancellationToken cancellationToken = default)
{
    using var httpRequest = BuildRequest(request, stream: false, tools);
    using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
    // ... error handling same as CompleteAsync ...

    var json = await response.Content.ReadAsStringAsync(cancellationToken);
    var root = JsonNode.Parse(json)!;
    var stopReason = root["stop_reason"]?.GetValue<string>();
    var usage = ParseUsage(root["usage"]);

    if (stopReason == "tool_use")
    {
        var contentArr = root["content"]!.AsArray();
        foreach (var block in contentArr)
        {
            if (block?["type"]?.GetValue<string>() == "tool_use")
            {
                var toolUseId = block["id"]!.GetValue<string>();
                var toolName = block["name"]!.GetValue<string>();
                var toolInput = JsonSerializer.Deserialize<JsonElement>(
                    block["input"]!.ToJsonString());
                return new AnthropicToolResult.ToolUseRequest(toolUseId, toolName, toolInput, usage);
            }
        }
    }

    // end_turn — extract text
    var textBuilder = new StringBuilder();
    if (root["content"] is JsonArray arr)
        foreach (var block in arr)
            if (block?["type"]?.GetValue<string>() == "text")
                textBuilder.Append(block["text"]?.GetValue<string>() ?? string.Empty);

    return new AnthropicToolResult.TextResponse(textBuilder.ToString(), usage);
}
```

### Injecting tools via DI (DependencyInjection.cs pattern)

```csharp
// In Diax.Application/DependencyInjection.cs
services.AddScoped<IAgentHandler, CommercialAgentHandler>();
// Phase 3 adds: services.AddScoped<IAgentHandler, SupportAgentHandler>();
// Phase 4 adds: services.AddScoped<IAgentHandler, PersonalAgentHandler>();
services.AddScoped<IAgentOrchestratorService, AgentOrchestratorService>();
// Orchestrator receives IEnumerable<IAgentHandler> — DI delivers all registered handlers
```

### Tool that requires confirmation (example for Phase 3+)

```csharp
// Pattern for Phase 3 tools — shown here for completeness
public class UpdateLeadStatusTool : IAgentTool
{
    public string Name => "update_lead_status";
    public bool RequiresConfirmation => true;

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, Guid userId, CancellationToken ct)
    {
        // For write tools: NEVER write here — return PendingConfirmation
        var leadId = input.GetProperty("lead_id").GetString()!;
        var newStatus = input.GetProperty("new_status").GetString()!;
        return new AgentToolResult.PendingConfirmation(
            ActionLabel: $"Atualizar lead {leadId} para {newStatus}",
            SerializedPayload: input.GetRawText());
    }

    public async Task<AgentToolResult> ExecuteConfirmedAsync(string payload, Guid userId, CancellationToken ct)
    {
        // Actual write happens HERE, after user confirmation
        var input = JsonSerializer.Deserialize<JsonElement>(payload);
        // ... call repository ...
        return new AgentToolResult.Ok("Lead atualizado com sucesso.");
    }
}
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual SSE parsing in AnthropicChatClient | Same — project uses raw HttpClient intentionally | N/A | No SDK dependency; add tool parsing in new method only |
| CommercialAgentService standalone (stateless) | CommercialAgentService adapts to IAgentHandler + orchestrator routing | Phase 2 | Enables all agents to share one /chat endpoint |
| AiConversation with no agent context | AiConversation with nullable AgentType column | Phase 2 migration | Agent conversations separated from AiChat conversations |

**Deprecated/outdated:**
- CommercialAgentService being called directly from AgentsController: remains for the `/commercial/chat` endpoint (backward compat), but the new `/agents/{type}/chat` route goes through the orchestrator.

---

## Open Questions

1. **AiUsageTracking: providerId and modelId are Guid (DB references)**
   - What we know: `IAiUsageTrackingService.LogUsageAsync` requires `Guid providerId, Guid modelId` — these are foreign keys to `AiProvider` and `AiModel` tables.
   - What's unclear: Agent orchestrator knows model name ("claude-sonnet-4-5") but may not have the provider/model GUIDs readily without a DB lookup.
   - Recommendation: Either (a) look up by model name in orchestrator (1 extra query per chat), or (b) make usage tracking optional in Phase 2 for agent endpoints (featureType only, skip providerId/modelId). Option (b) is simpler for Phase 2.

2. **GroupAiAccess validation for agents: which providerKey/modelKey?**
   - What we know: `BaseAiController.ExecuteAiActionAsync` calls `_catalogService.ValidateUserAccessAsync(userId, providerKey, modelKey, ct)`.
   - What's unclear: Agent orchestrator hardcodes "claude-sonnet-4-5" — does the RBAC catalog have a matching entry for this model?
   - Recommendation: Planner should specify that the agent controller uses the same providerKey/modelKey as existing AiChat (likely "anthropic" / "claude-sonnet-4-5"). If the existing RBAC seeder already seeds this, no action needed.

---

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.x + Moq + FluentAssertions |
| Config file | `api-core/tests/Diax.Tests/Diax.Tests.csproj` |
| Quick run command | `cd api-core && dotnet test --filter "FullyQualifiedName~Agents"` |
| Full suite command | `cd api-core && dotnet test` |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| ORCH-01 | Orchestrator routes AgentType.Commercial to CommercialAgentHandler | unit | `dotnet test --filter "AgentOrchestratorServiceTests"` | Wave 0 |
| ORCH-01 | Orchestrator returns Error("NotFound") for unknown AgentType | unit | `dotnet test --filter "AgentOrchestratorServiceTests"` | Wave 0 |
| ORCH-02 | All agents send same model/client; system prompt differs per type | unit | `dotnet test --filter "AgentOrchestratorServiceTests"` | Wave 0 |
| ORCH-03 | RBAC check is called on every /chat request | unit | `dotnet test --filter "AgentsControllerTests"` | Wave 0 |
| ORCH-04 | Write tool returns PendingConfirmation — NOT executed immediately | unit | `dotnet test --filter "IAgentToolTests"` | Wave 0 |
| ORCH-04 | /confirm endpoint executes write and marks action as executed | unit | `dotnet test --filter "AgentOrchestratorConfirmTests"` | Wave 0 |
| ORCH-04 | Expired pending action returns 400 on /confirm | unit | `dotnet test --filter "AgentPendingActionTests"` | Wave 0 |
| ORCH-05 | New conversation gets AgentType persisted | unit | `dotnet test --filter "AgentConversationTests"` | Wave 0 |
| ORCH-05 | Existing conversation is loaded and history included in LLM call | unit | `dotnet test --filter "AgentOrchestratorServiceTests"` | Wave 0 |
| ORCH-05 | AiChat conversations (AgentType=null) not returned by agent /conversations | unit | `dotnet test --filter "AgentConversationTests"` | Wave 0 |

**Critical invariant test (no data written before confirmation):**
```csharp
[Fact]
public async Task WriteToolExecuteAsync_NeverCallsRepository()
{
    // Given: a write tool with RequiresConfirmation = true
    // When: ExecuteAsync is called (not ExecuteConfirmedAsync)
    // Then: no repository method is called
    _customerRepo.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
}
```

### Sampling Rate
- **Per task commit:** `cd api-core && dotnet test --filter "FullyQualifiedName~Agents"`
- **Per wave merge:** `cd api-core && dotnet test`
- **Phase gate:** Full suite green before `/gsd:verify-work`

### Wave 0 Gaps (test files to create)
- [ ] `api-core/tests/Diax.Tests/Application/Agents/AgentOrchestratorServiceTests.cs` — ORCH-01, ORCH-02, ORCH-05
- [ ] `api-core/tests/Diax.Tests/Application/Agents/AgentPendingActionTests.cs` — ORCH-04 (domain entity invariants)
- [ ] `api-core/tests/Diax.Tests/Application/Agents/AgentConversationTests.cs` — ORCH-05 (AgentType persistence)
- [ ] `api-core/tests/Diax.Tests/Application/Agents/AgentsControllerTests.cs` — ORCH-03 (RBAC)

Existing: `CommercialAgentServiceTests.cs` (4 tests, passing) — must remain green throughout Phase 2.

---

## Sources

### Primary (HIGH confidence)
- Official Anthropic docs — tool-use wire format, stop_reason, tool_result constraints (verified 2026-05-28): https://platform.claude.com/docs/en/agents-and-tools/tool-use/define-tools and https://platform.claude.com/docs/en/agents-and-tools/tool-use/handle-tool-calls
- `AnthropicChatClient.cs` (read directly) — existing implementation, `BuildRequestBody`, `ParseEvent`, header format
- `IAnthropicChatClient.cs` (read directly) — exact method signatures to extend
- `AiChatService.cs` (read directly) — streaming pattern, message persistence, cost calculation
- `AiConversation.cs` (read directly) — entity structure, nullable fields
- `CommercialAgentService.cs` (read directly) — existing agent pattern to replicate/adapt
- `AgentsController.cs` (read directly) — existing endpoint contract that must not break
- `BaseAiController.cs` (read directly) — RBAC pattern
- `CommercialAgentServiceTests.cs` (read directly) — test pattern to follow

### Secondary (MEDIUM confidence)
- Pattern for DI registration of multiple handlers via `IEnumerable<T>` is standard .NET DI — HIGH confidence from .NET documentation.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new libraries; all reuse verified by direct code reading
- Architecture patterns: HIGH — Anthropic wire format verified against official docs; C# patterns follow existing project conventions verified by code reading
- Pitfalls: HIGH — most derived directly from reading the existing code (AnthropicChatClient, AiChatService) and Anthropic API constraints

**Research date:** 2026-05-28
**Valid until:** 2026-07-28 (Anthropic API version `2023-06-01` is stable; .NET 8 Clean Architecture patterns are stable)
