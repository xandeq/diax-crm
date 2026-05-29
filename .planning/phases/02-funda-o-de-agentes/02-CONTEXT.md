# Phase 2: Fundação de Agentes - Context

**Gathered:** 2026-05-28
**Status:** Ready for planning
**Source:** Captured by orchestrator from milestone context (no separate discuss-phase)

<domain>
## Phase Boundary

Entrega a **plataforma de orquestração de agentes** no backend .NET — base para os agentes Comercial (Phase 3), Suporte (Phase 4) e Pessoal (Phase 5). Sem UI nesta fase (UI é Phase 6).

**Inclui:** AgentOrchestrator (roteamento por tipo), framework de tools/function-calling reutilizável com confirmação obrigatória para escrita, persistência/retomada de conversas (reuso AiConversation), RBAC (GroupAiAccess) e logging de uso (AiUsageTracking).

**NÃO inclui:** lógica específica de cada agente (prompts/tools de Comercial/Suporte/Pessoal vêm nas fases seguintes); nenhuma UI.
</domain>

<decisions>
## Implementation Decisions (LOCKED)

### Reaproveitamento (sem quebrar nada)
- Motor de chat ÚNICO reaproveitando `IAnthropicChatClient` / `AiChatService`. NÃO criar novo provider LLM.
- O endpoint já existente `POST /api/v1/agents/commercial/chat` (CommercialAgentService) NÃO pode quebrar. O orquestrador deve acomodá-lo ou o Commercial deve passar a rotear por ele sem mudança de contrato externo.
- `AgentType` enum já existe (Personal=0, Support=1, Commercial=2) em `Diax.Domain/Agents`.
- Persistência reusa `AiConversation` (entidade/-repos de AiChat), associando o tipo de agente à conversa.
- RBAC reusa `GroupAiAccess` (validação já usada em BaseAiController/AiCatalog). Uso reusa `AiUsageTracking`.
- Padrões a seguir: Result pattern + `HandleResult`, `BaseApiController`/`GetCurrentUserId()`, services em `Diax.Application/<Feature>` com interface, DTOs em subpasta, registro em `Diax.Application/DependencyInjection.cs`, testes Moq+xUnit.

### Function-calling / Tools
- O `IAnthropicChatClient` atual SÓ faz texto (StreamMessageAsync + CompleteAsync) — NÃO suporta tool-use. Esta fase precisa decidir como adicionar tool-use SEM quebrar o AiChat existente (ex.: estender o request/cliente de forma opt-in, ou um caminho separado). A pesquisa deve resolver isso.
- Framework de tools reutilizável entre agentes: cada agente declara as tools que expõe; o orquestrador injeta as tools do tipo no request.
- **Toda tool que GRAVA dados** retorna uma ação pendente (`pending_confirmation`) com payload — nada é gravado até confirmação. Tools somente-leitura podem executar direto.
- Confirmação via `POST /api/v1/agents/{type}/confirm` (ou equivalente) que executa a ação pendente e continua a conversa.

### Segurança / Princípios
- Sem inventar dados — agentes só usam o que está no CRM.
- Ação de escrita SEM confirmação é Out of Scope (proibido).

### Claude's Discretion
- Forma exata de modelar a "ação pendente" (tabela nova vs. estado em memória/conversa vs. token assinado) — preferir a opção que evite migration pesada se possível; se exigir migration, usar `update-db.ps1`.
- Assinatura concreta das interfaces `IAgentOrchestrator`, `IAgentTool`, `IAgentHandler`.
- Como associar `AgentType` à `AiConversation` (coluna nova vs. metadado) — avaliar impacto de migration.
</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Motor de chat / LLM existente (reuso)
- `api-core/src/Diax.Application/AiChat/AiChatService.cs` — engine de chat, streaming, custo, persistência
- `api-core/src/Diax.Application/AiChat/IAnthropicChatClient.cs` — contrato LLM atual (texto only; sem tools)
- `api-core/src/Diax.Domain/AiChat/` — `AiConversation`, `AiChatMessage`, repositório

### Agente Comercial já construído (não quebrar)
- `api-core/src/Diax.Api/Controllers/V1/AgentsController.cs`
- `api-core/src/Diax.Application/Agents/Commercial/CommercialAgentService.cs`
- `api-core/src/Diax.Domain/Agents/AgentType.cs`

### Infra transversal (reuso)
- `api-core/src/Diax.Api/Controllers/BaseAiController.cs` — RBAC `ValidateUserAccessAsync`, error handling
- `api-core/src/Diax.Application/AI/Services/AiUsageTrackingService.cs` — logging de uso
- `api-core/src/Diax.Application/AI/GroupAiAccessService.cs` — acesso por grupo
- `api-core/src/Diax.Application/DependencyInjection.cs` — registro de services
- `CLAUDE.md` (raiz) + protocolo `/wave-qa`
</canonical_refs>

<specifics>
## Specific Ideas

- Endpoints alvo: `POST /api/v1/agents/{type}/chat`, `POST /api/v1/agents/{type}/confirm`, `GET /api/v1/agents/{type}/conversations`, `GET .../conversations/{id}`.
- Manter `claude-sonnet-4-5` como modelo default dos agentes (já usado no Commercial).
</specifics>

<deferred>
## Deferred Ideas

- Tools específicas de cada agente (Comercial/Suporte/Pessoal) — fases 3/4/5.
- UI — Phase 6.
- Streaming com tool-use no frontend — Phase 6 (esta fase pode entregar non-streaming para tool-use se simplificar).
</deferred>

---

*Phase: 02-funda-o-de-agentes*
*Context gathered: 2026-05-28 by orchestrator*
