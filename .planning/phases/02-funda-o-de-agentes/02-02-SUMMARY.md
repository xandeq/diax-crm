---
phase: 02-funda-o-de-agentes
plan: 02
subsystem: AiChat LLM Client
tags: [anthropic, tool-use, function-calling, non-streaming, tdd]
dependency_graph:
  requires: []
  provides: [CompleteWithToolsAsync, AnthropicToolDefinition, AnthropicToolResult]
  affects: [IAnthropicChatClient, AnthropicChatClient]
tech_stack:
  added: []
  patterns: [discriminated-union-records, mocked-HttpMessageHandler-tests, TDD-red-green]
key_files:
  created:
    - api-core/tests/Diax.Tests/Application/AiChat/AnthropicToolUseTests.cs
  modified:
    - api-core/src/Diax.Application/AiChat/IAnthropicChatClient.cs
    - api-core/src/Diax.Infrastructure/AiChat/AnthropicChatClient.cs
decisions:
  - "CompleteWithToolsAsync is a third opt-in method — existing StreamMessageAsync and CompleteAsync signatures and request bodies are byte-identical when no tools are passed"
  - "Tools array serialization gated by tools is { Count: > 0 } if-block so no-tools path is provably unchanged"
  - "Used System.Text.Json JsonElement for InputSchema and ToolInput (no new NuGet packages)"
  - "AnthropicToolResult is abstract record with nested TextResponse / ToolUseRequest — discriminated union via C# pattern matching"
metrics:
  duration: "~8 minutes"
  completed_date: "2026-05-29"
  tasks_completed: 2
  files_changed: 3
---

# Phase 02 Plan 02: AnthropicChatClient Tool-Use Extension Summary

**One-liner:** Non-breaking `CompleteWithToolsAsync` added to `IAnthropicChatClient` with `AnthropicToolDefinition` + `AnthropicToolResult` discriminated union, parsing `stop_reason` into `ToolUseRequest` or `TextResponse`.

## What Was Built

Extended the existing raw-HttpClient `AnthropicChatClient` with an opt-in third method that supports Anthropic tool-use (function-calling). The existing `StreamMessageAsync` and `CompleteAsync` paths are completely untouched.

### New Interface Members (IAnthropicChatClient.cs)

```csharp
// Third method — opt-in, non-breaking:
Task<AnthropicToolResult> CompleteWithToolsAsync(
    AnthropicMessageRequest request,
    IReadOnlyList<AnthropicToolDefinition> tools,
    CancellationToken cancellationToken = default);

// Tool definition type:
public record AnthropicToolDefinition(
    string Name,
    string Description,
    JsonElement InputSchema);  // pre-serialized JSON Schema object

// Result discriminated union:
public abstract record AnthropicToolResult
{
    public record TextResponse(string Text, AnthropicUsage Usage) : AnthropicToolResult;
    public record ToolUseRequest(
        string ToolUseId,
        string ToolName,
        JsonElement ToolInput,
        AnthropicUsage Usage) : AnthropicToolResult;
}
```

### Implementation (AnthropicChatClient.cs)

- `BuildRequestBody` extended with optional `IReadOnlyList<AnthropicToolDefinition>? tools = null` parameter
- Tools array serialized via `if (tools is { Count: > 0 })` guard — no-tools body is provably identical
- Each tool entry: `{ "name", "description", "input_schema": JsonNode.Parse(t.InputSchema.GetRawText()) }`
- `CompleteWithToolsAsync` reuses `BuildRequest`, `ParseUsage`, and error handling from `CompleteAsync`
- Response parsing: `stop_reason == "tool_use"` → iterate content[], find `type=="tool_use"` block → return `ToolUseRequest`; else → concatenate `type=="text"` blocks → return `TextResponse`

### Tests (AnthropicToolUseTests.cs)

Two `[Fact]` tests using mocked `HttpMessageHandler` (canned JSON, no real HTTP):
1. `CompleteWithToolsAsync_WhenStopReasonIsToolUse_ReturnsToolUseRequest` — verifies `ToolUseId`, `ToolName`, `ToolInput` properties and usage parsing
2. `CompleteWithToolsAsync_WhenStopReasonIsEndTurn_ReturnsTextResponse` — verifies concatenated text and usage parsing

## Verification

- `dotnet build Diax.CRM.sln` — 0 errors
- `AnthropicToolUse` filter: 2/2 pass
- `AiChat` filter (all AiChat tests): 8/8 pass (6 pre-existing + 2 new)

## Deviations from Plan

**Coordination note:** Plan 02-01 (parallel wave agent) also committed changes to `AnthropicChatClient.cs` as part of their commit `2e57b6b`. The implementation was committed there; our plan's contribution is the interface extension (`IAnthropicChatClient.cs`) and the test file (`AnthropicToolUseTests.cs`) committed in `cb8bfa4`. Both plans' deliverables are present and all tests pass.

No other deviations — plan executed as written.

## Known Stubs

None. The `CompleteWithToolsAsync` method is fully wired — real HTTP call with tool definitions, real response parsing by `stop_reason`.

## Plan 03 Usage Guide

Plan 03 (AgentOrchestratorService) should:

1. Inject `IAnthropicChatClient` and call `CompleteWithToolsAsync` when `handler.GetTools().Count > 0`
2. Build tool definitions: `new AnthropicToolDefinition(tool.Name, tool.Description, tool.InputSchema)`
3. Switch on result:
   ```csharp
   return toolResult switch {
       AnthropicToolResult.TextResponse text => /* persist + return text */,
       AnthropicToolResult.ToolUseRequest toolUse => /* insert AgentPendingAction + return pending DTO */,
       _ => Result.Failure(...)
   };
   ```
4. Tool continuation (second API call with tool_result) uses existing `CompleteAsync` with a user message containing the `tool_result` content block

## Self-Check: PASSED

- IAnthropicChatClient.cs — FOUND, contains CompleteWithToolsAsync + AnthropicToolDefinition + AnthropicToolResult
- AnthropicChatClient.cs — FOUND, contains implementation
- AnthropicToolUseTests.cs — FOUND, 2 [Fact] tests
- Commit cb8bfa4 — FOUND (test + interface)
- Build: 0 errors
- Tests: 8/8 pass (AiChat filter)
