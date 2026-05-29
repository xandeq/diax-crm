# Requirements: DIAX CRM — v1.2 Agentes de IA

**Defined:** 2026-05-28
**Core Value:** Centralizar todas as operações de negócio em um único sistema pessoal

> Princípios do milestone: reaproveitar a infra de IA existente (IAnthropicChatClient, AiChatService,
> ICustomerRepository, OutreachService, ITicketService, IAppointmentService, FinancialSummaryService,
> GroupAiAccess, AiUsageTracking) **sem quebrar nada**; **não inventar dados** (toda resposta baseada
> no que está no CRM); **ações de escrita exigem confirmação** do usuário; UI sempre bonita (/impeccable).

## v1.2 Requirements

### Orquestração & Fundação (ORCH)

- [ ] **ORCH-01**: AgentOrchestrator roteia a conversa para o handler do tipo de agente correto (Comercial/Suporte/Pessoal)
- [x] **ORCH-02**: Cada agente compartilha o mesmo motor de chat, variando apenas system prompt, ferramentas e escopo de dados
- [ ] **ORCH-03**: Acesso aos agentes respeita RBAC/grupos (reuso GroupAiAccess) e o uso é registrado (reuso AiUsageTracking)
- [ ] **ORCH-04**: Framework de tools (function-calling) reutilizável entre agentes; toda tool que grava dados retorna uma ação pendente que exige confirmação do usuário antes de executar
- [ ] **ORCH-05**: Conversas dos agentes são persistidas e retomáveis (reuso de AiConversation, com o tipo de agente associado)

### Agente Comercial (CMRC)

- [ ] **CMRC-01**: Usuário conversa com o Agente Comercial e recebe respostas baseadas no pipeline real de leads *(construído — stateless)*
- [ ] **CMRC-02**: Usuário anexa leads ao contexto por IDs ou por segmento (Hot/Warm/Cold) *(construído)*
- [ ] **CMRC-03**: Agente prioriza leads por probabilidade de conversão (score/segmento/estágio) *(construído)*
- [ ] **CMRC-04**: Agente gera rascunho de abordagem (e-mail/WhatsApp) personalizado por lead, reaproveitando padrões de OutreachService/AiOutreachAbTest
- [ ] **CMRC-05**: Agente pode atualizar status e/ou segmento de um lead via ação confirmada
- [ ] **CMRC-06**: Conversa do Agente Comercial é persistida e retomável (depende de ORCH-05)

### Agente de Suporte (SUP)

- [ ] **SUP-01**: Usuário conversa com o Agente de Suporte com contexto do histórico do cliente (timeline/contatos)
- [ ] **SUP-02**: Agente sugere resposta de atendimento baseada no histórico e na dúvida apresentada
- [ ] **SUP-03**: Agente resume sob demanda o histórico de relacionamento de um cliente
- [ ] **SUP-04**: Agente pode abrir/triar um ticket (reuso de ITicketService) via ação confirmada

### Agente Pessoal (PERS)

- [ ] **PERS-01**: Usuário conversa com o Agente Pessoal sobre sua agenda e finanças
- [ ] **PERS-02**: Agente resume a agenda do dia/semana (reuso de IAppointmentService)
- [ ] **PERS-03**: Agente entrega snapshot financeiro sob demanda (reuso de FinancialSummaryService)
- [ ] **PERS-04**: Agente cria um compromisso na agenda via ação confirmada (reuso de AppointmentService)

### UI dos Agentes (AGUI)

- [ ] **AGUI-01**: Página `/agentes` no crm-web com seletor de agente (Comercial/Suporte/Pessoal)
- [ ] **AGUI-02**: Componente de chat reaproveitado de `ai-chat`, com streaming da resposta
- [ ] **AGUI-03**: UI exibe o contexto anexado (leads/cliente) e o custo/uso de tokens da conversa
- [ ] **AGUI-04**: Ações sugeridas pelo agente aparecem como botões de confirmação (ex.: "Atualizar status", "Abrir ticket", "Criar compromisso")
- [ ] **AGUI-05**: UI consistente com o design system (shadcn/Tailwind), responsiva e com microinterações (Framer Motion) — padrão /impeccable

## Deferido (v1.1 — Produtividade Pessoal, superado)

Planejado em 2026-04-03, nunca executado pelo GSD. Mantido como backlog; reavaliar em milestone futuro.

### Morning Briefing / Tarefas / Pipeline / Propostas
- **BRIEF-01..04**, **TASK-01..05**, **PIPE-01..05**, **PROP-01..07** — ver histórico abaixo

## Out of Scope

| Feature | Reason |
|---------|--------|
| Agente executar ação de escrita sem confirmação | Risco de gravar dados indevidos; toda escrita exige aprovação |
| Agente que inventa/estima dados ausentes | Princípio "sem inventar" — agente só usa o que está no CRM |
| Novo provider LLM próprio para agentes | Reaproveita IAnthropicChatClient existente |
| Portal do cliente / acesso externo aos agentes | Sistema single-user |
| Voz/áudio nos agentes | Fora do escopo deste milestone |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| ORCH-01 | Phase 2 | Pending |
| ORCH-02 | Phase 2 | Complete |
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

**Coverage:**
- v1.2 requirements: 24 total (CMRC-01..03 já construídos, a validar na Phase 3)
- Mapped to phases: 24/24
- Unmapped: 0

---
*Requirements defined: 2026-05-28 — milestone v1.2 Agentes de IA*
*Traceability filled: 2026-05-28 — roadmap created*
*v1.1 (Produtividade Pessoal) deferido — ver MILESTONES.md*
