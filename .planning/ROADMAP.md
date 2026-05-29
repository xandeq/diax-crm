# Roadmap — DIAX CRM

## Milestones

- ✅ **v1.0 Base** — Plataforma completa em produção (shipped antes de 2026-04-03)
- ⚠️ **v1.1 Produtividade Pessoal** — Superado (planejado 2026-04-03, nunca executado pelo GSD; código evoluiu via sprints)
- 🚧 **v1.2 Agentes de IA** — Phases 2-6 (current)

---

## v1.1 — Produtividade Pessoal (superado)

<details>
<summary>Phase 1: Tarefas (v1.1 — 01-tarefas)</summary>

**Status:** Superado — v1.1 nunca executado pelo GSD. Phase 1 planejada mas não executada via este roadmap.

**Requirements:** TASK-01..05, PIPE-01..05, PROP-01..07, BRIEF-01..04

Detalhes preservados abaixo para referência histórica.

### Phase 1: Tarefas
**Goal**: Usuário pode gerenciar suas tarefas pessoais com prazo, prioridade e status
**Requirements**: TASK-01, TASK-02, TASK-03, TASK-04, TASK-05
**Success Criteria** (what must be TRUE):
  1. Usuário pode criar uma tarefa com título, descrição opcional, prazo e prioridade (baixa/média/alta)
  2. Usuário pode marcar uma tarefa como concluída e revertê-la para pendente
  3. Usuário pode editar e excluir qualquer tarefa existente
  4. Usuário pode filtrar a lista de tarefas por status (pendente/concluída) e por prioridade
  5. Contagem de tarefas pendentes do dia aparece visível no header ou no dashboard
**Plans**: TBD (superado)

</details>

---

## v1.2 — Agentes de IA (current)

**Milestone Goal:** Transformar o CRM de repositório de dados em parceiro de execução por IA — três agentes (Comercial, Suporte, Pessoal) que conversam usando os dados reais do CRM e executam ações sob confirmação, reaproveitando a infra de IA existente sem quebrar nada.

**Coverage:** 24/24 requirements mapped

### Phases

- [ ] **Phase 2: Fundação de Agentes** — AgentOrchestrator, framework de tools com confirmação, persistência de conversas (ORCH-01..05)
- [ ] **Phase 3: Agente Comercial** — Validar e completar o agente já parcialmente construído; tools de outreach draft e atualização de lead (CMRC-01..06)
- [ ] **Phase 4: Agente de Suporte** — Contexto do histórico do cliente, sugestão de resposta, resumo e abertura de ticket (SUP-01..04)
- [ ] **Phase 5: Agente Pessoal** — Agenda e finanças reais, criação de compromisso via ação confirmada (PERS-01..04)
- [ ] **Phase 6: UI /agentes** — Página unificada com seletor de agente, streaming chat, contexto anexado, botões de confirmação, design /impeccable (AGUI-01..05)

---

## Phase Details

### Phase 2: Fundação de Agentes

**Goal:** A plataforma de orquestração de agentes existe — qualquer agente pode ser conectado compartilhando o mesmo motor de chat, diferindo apenas por prompt/tools/escopo, com toda ação de escrita protegida por confirmação e conversas persistidas

**Depends on:** Nothing (sobre infra existente: IAnthropicChatClient, AiChatService, AiConversation, GroupAiAccess, AiUsageTracking)

**Requirements:** ORCH-01, ORCH-02, ORCH-03, ORCH-04, ORCH-05

**Success Criteria** (what must be TRUE):
  1. Uma requisição para `POST /api/v1/agents/{type}/chat` com type=Commercial|Support|Personal é roteada pelo AgentOrchestrator para o handler correto sem quebrar o endpoint existente de Commercial
  2. Ao executar uma tool-call que grava dados, a API retorna resposta com estado `pending_confirmation` e payload da ação — nenhum dado é gravado até a confirmação chegar via `POST /api/v1/agents/{type}/confirm`
  3. Ao confirmar a ação pendente, o dado é gravado e a resposta do agente confirma a execução
  4. O histórico de uma conversa de agente é salvo em AiConversation com o tipo de agente associado; `GET /api/v1/agents/{type}/conversations` lista as conversas; retomar uma conversa preserva o contexto anterior
  5. Usuário sem permissão no grupo correto recebe 403; uso de tokens de cada conversa é registrado em AiUsageTracking

**Plans:** 4 plans (2 waves)
- [ ] 02-01-PLAN.md — Persistence foundation: AgentPendingAction entity + repo, AgentType nullable on AiConversation, migrations (Wave 1, ORCH-04/05)
- [ ] 02-02-PLAN.md — LLM tool-use: CompleteWithToolsAsync on IAnthropicChatClient (non-breaking, Wave 1, ORCH-02)
- [ ] 02-03-PLAN.md — Orchestrator + tool framework: IAgentTool/IAgentHandler/IAgentOrchestratorService + CommercialAgentHandler (Wave 2, ORCH-01/02/04/05)
- [ ] 02-04-PLAN.md — Controller endpoints + RBAC + usage logging (Wave 3, ORCH-01/03/04/05)

---

### Phase 3: Agente Comercial

**Goal:** O Agente Comercial é completo e production-ready — conversa sobre o pipeline real, prioriza leads, gera rascunhos de outreach personalizados e atualiza status/segmento via ação confirmada, com conversas persistidas

**Depends on:** Phase 2

**Note on pre-built items:** CMRC-01 (chat com pipeline real), CMRC-02 (anexar leads por ID/segmento) e CMRC-03 (priorização por score/segmento/estágio) já estão construídos fora do GSD (stateless, sem persistência, 4 testes passando). Esta fase valida, integra ao framework ORCH e adiciona as capabilities faltantes (CMRC-04, CMRC-05, CMRC-06).

**Requirements:** CMRC-01, CMRC-02, CMRC-03, CMRC-04, CMRC-05, CMRC-06

**Success Criteria** (what must be TRUE):
  1. Usuário pergunta "quais leads quentes devo priorizar hoje?" e recebe lista ranqueada baseada nos dados reais de Customer do CRM (score, segmento, estágio)
  2. Usuário pede rascunho de abordagem para o lead X e recebe e-mail ou mensagem WhatsApp personalizado gerado com base nos padrões de OutreachService/AiOutreachAbTest — nenhuma mensagem é enviada sem ação explícita do usuário
  3. Agente sugere atualizar status/segmento de um lead → retorna ação `pending_confirmation` → ao confirmar, o Customer é atualizado no banco e a resposta do agente confirma a ação
  4. Conversa do Agente Comercial é salva e retomável: retomar conversa anterior traz de volta o contexto dos leads discutidos
  5. Os 4 testes unitários existentes continuam passando; nenhum endpoint existente é quebrado

**Plans:** TBD

---

### Phase 4: Agente de Suporte

**Goal:** O Agente de Suporte opera com pleno conhecimento do histórico de um cliente — responde dúvidas baseado em dados reais, sugere respostas de atendimento, resume relacionamentos e abre tickets via ação confirmada

**Depends on:** Phase 2

**Requirements:** SUP-01, SUP-02, SUP-03, SUP-04

**Success Criteria** (what must be TRUE):
  1. Usuário informa ID ou nome de um cliente e o agente responde usando o histórico real da timeline (contatos, interações, notas) — sem inventar dados ausentes
  2. Dado um relato de dúvida ou problema do cliente, o agente gera sugestão de resposta de atendimento coerente com o histórico; o texto é copiável e não é enviado automaticamente
  3. Usuário pede "resumo do cliente X" e recebe resumo estruturado (últimas interações, status, pontos abertos) baseado na timeline real
  4. Agente sugere abrir ticket → retorna ação `pending_confirmation` com título/categoria pré-preenchidos → ao confirmar, ITicketService cria o ticket e o agente confirma com o ID criado

**Plans:** TBD

---

### Phase 5: Agente Pessoal

**Goal:** O Agente Pessoal é o assistente executivo do Alexandre — consulta agenda real e snapshot financeiro real, e cria compromissos via ação confirmada

**Depends on:** Phase 2

**Requirements:** PERS-01, PERS-02, PERS-03, PERS-04

**Success Criteria** (what must be TRUE):
  1. Usuário pergunta "o que tenho hoje?" e recebe a lista real de compromissos do dia vinda de IAppointmentService, com horários e títulos corretos
  2. Usuário pede "resumo financeiro" e recebe snapshot real do FinancialSummaryService (saldo, receitas/despesas do mês, alertas de metas) — sem dados inventados
  3. Usuário dita um compromisso em linguagem natural ("agendar reunião com João sexta às 14h") → agente extrai data/hora/título → retorna ação `pending_confirmation` → ao confirmar, AppointmentService cria o compromisso e o agente confirma com data/hora formatados
  4. Conversa do Agente Pessoal é persistida: retomar conversa anterior preserva o contexto de agenda e finanças discutido

**Plans:** TBD

---

### Phase 6: UI /agentes

**Goal:** A página /agentes existe como interface unificada e bonita para todos os três agentes — seletor de agente, chat com streaming real, contexto anexado visível, botões de confirmação para ações pendentes, totalmente consistente com o design system

**Depends on:** Phase 3, Phase 4, Phase 5

**UI Rationale:** Toda a UI é construída em uma fase única para garantir consistência visual e máxima reutilização do componente de chat de ai-chat. Construir UI parcial em fases anteriores criaria inconsistência e retrabalho. As fases 3-5 são validáveis inteiramente via testes de API e /wave-qa.

**Requirements:** AGUI-01, AGUI-02, AGUI-03, AGUI-04, AGUI-05

**Success Criteria** (what must be TRUE):
  1. Usuário acessa `/agentes`, vê três cards (Comercial / Suporte / Pessoal), clica em um e entra no chat daquele agente — sem recarregar a página
  2. A resposta do agente aparece em streaming (token a token) com o mesmo comportamento visual já existente no ai-chat
  3. O painel lateral (ou header) exibe o contexto anexado à conversa (leads selecionados, cliente ativo, custo/uso de tokens) de forma legível e atualizada em tempo real
  4. Quando o agente retorna uma ação pendente, botões "Confirmar" e "Cancelar" aparecem no chat — ao clicar Confirmar a ação é executada e o resultado aparece na conversa; ao cancelar, a ação é descartada
  5. A página passa na inspeção visual /impeccable: responsiva em mobile/desktop, microinterações com Framer Motion, paleta e tipografia idênticas ao restante do CRM, dark mode consistente

**Plans:** TBD

---

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 2. Fundação de Agentes | 0/4 | Not started | — |
| 3. Agente Comercial | 0/? | Not started | — |
| 4. Agente de Suporte | 0/? | Not started | — |
| 5. Agente Pessoal | 0/? | Not started | — |
| 6. UI /agentes | 0/? | Not started | — |

---

## Coverage Map

| Requirement | Phase | Status |
|-------------|-------|--------|
| ORCH-01 | Phase 2 | Pending |
| ORCH-02 | Phase 2 | Pending |
| ORCH-03 | Phase 2 | Pending |
| ORCH-04 | Phase 2 | Pending |
| ORCH-05 | Phase 2 | Pending |
| CMRC-01 | Phase 3 | Pending |
| CMRC-02 | Phase 3 | Pending |
| CMRC-03 | Phase 3 | Pending |
| CMRC-04 | Phase 3 | Pending |
| CMRC-05 | Phase 3 | Pending |
| CMRC-06 | Phase 3 | Pending |
| SUP-01 | Phase 4 | Pending |
| SUP-02 | Phase 4 | Pending |
| SUP-03 | Phase 4 | Pending |
| SUP-04 | Phase 4 | Pending |
| PERS-01 | Phase 5 | Pending |
| PERS-02 | Phase 5 | Pending |
| PERS-03 | Phase 5 | Pending |
| PERS-04 | Phase 5 | Pending |
| AGUI-01 | Phase 6 | Pending |
| AGUI-02 | Phase 6 | Pending |
| AGUI-03 | Phase 6 | Pending |
| AGUI-04 | Phase 6 | Pending |
| AGUI-05 | Phase 6 | Pending |

**Total mapped: 24/24**

---

*Roadmap updated: 2026-05-28 — v1.2 Agentes de IA (5 phases, 24 requirements). Phase 2 planned: 4 plans, 3 waves.*
*Previous: v1.1 Produtividade Pessoal (superado — Phase 1 / 01-tarefas)*
