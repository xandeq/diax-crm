# Requirements: DIAX CRM

**Defined:** 2026-04-03
**Core Value:** Centralizar todas as operações de negócio em um único sistema pessoal

## v1.1 Requirements — Produtividade Pessoal

### Morning Briefing

- [ ] **BRIEF-01**: Usuário vê agenda de compromissos de hoje ao acessar o dashboard
- [ ] **BRIEF-02**: Usuário vê lista de leads Warm/Hot sem contato nos últimos 7 dias
- [ ] **BRIEF-03**: Usuário vê tarefas do dia (com prazo = hoje ou vencidas)
- [ ] **BRIEF-04**: Usuário vê snapshot financeiro: saldo total das contas + receitas e despesas do mês corrente

### Tarefas

- [ ] **TASK-01**: Usuário pode criar tarefa com título, descrição opcional, prazo e prioridade (baixa/média/alta)
- [ ] **TASK-02**: Usuário pode marcar tarefa como concluída
- [ ] **TASK-03**: Usuário pode editar e excluir tarefas
- [ ] **TASK-04**: Usuário pode filtrar tarefas por status (pendente/concluída) e prioridade
- [ ] **TASK-05**: Usuário vê contagem de tarefas pendentes do dia no header ou dashboard

### Pipeline Kanban

- [ ] **PIPE-01**: Usuário vê leads organizados em colunas por etapa: Lead → Contatado → Qualificado → Negociando → Cliente
- [ ] **PIPE-02**: Usuário pode mover um lead entre etapas arrastando o card (drag-and-drop)
- [ ] **PIPE-03**: Card do lead mostra nome, empresa, segmento (Hot/Warm/Cold) e última interação
- [ ] **PIPE-04**: Usuário pode clicar num card para abrir o detalhe do lead
- [ ] **PIPE-05**: Usuário vê contagem de leads por coluna

### Propostas

- [ ] **PROP-01**: Usuário pode criar e gerenciar templates de proposta com nome, itens de serviço padrão e valores
- [ ] **PROP-02**: Usuário pode criar uma proposta a partir de um template, preenchendo os dados do cliente
- [ ] **PROP-03**: Proposta contém: dados do cliente (nome, empresa, contato), itens de serviço (descrição, valor unitário, quantidade), total calculado, data de validade e linha de assinatura
- [ ] **PROP-04**: Usuário pode editar os itens de uma proposta antes de finalizar
- [ ] **PROP-05**: Usuário pode gerar o texto/conteúdo da proposta com IA baseado no cliente e serviços selecionados
- [ ] **PROP-06**: Usuário pode exportar a proposta como PDF
- [ ] **PROP-07**: Usuário pode vincular uma proposta a um cliente/lead existente

## v2 Requirements

### Propostas — Avançado

- **PROP-08**: Rastreamento de abertura do PDF (link único por proposta)
- **PROP-09**: Assinatura digital com valor legal
- **PROP-10**: Envio da proposta diretamente por e-mail a partir do CRM

### Agenda — Avançado

- **AGENDA-01**: Sincronização bidirecional com Google Calendar
- **AGENDA-02**: Lembretes por e-mail ou WhatsApp antes do compromisso

### Tarefas — Avançado

- **TASK-06**: Tarefas vinculadas a cliente/lead
- **TASK-07**: Tarefas recorrentes (ex: todo mês revisar finanças)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Portal do cliente | Sistema single-user — clientes não têm acesso |
| App mobile nativo | Web-first é suficiente, defer v2+ |
| Integração Google Calendar | Complexidade bidirecional, defer v2 |
| Assinatura digital legal | Requer integração jurídica, defer v2 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| BRIEF-01 | — | Pending |
| BRIEF-02 | — | Pending |
| BRIEF-03 | — | Pending |
| BRIEF-04 | — | Pending |
| TASK-01 | — | Pending |
| TASK-02 | — | Pending |
| TASK-03 | — | Pending |
| TASK-04 | — | Pending |
| TASK-05 | — | Pending |
| PIPE-01 | — | Pending |
| PIPE-02 | — | Pending |
| PIPE-03 | — | Pending |
| PIPE-04 | — | Pending |
| PIPE-05 | — | Pending |
| PROP-01 | — | Pending |
| PROP-02 | — | Pending |
| PROP-03 | — | Pending |
| PROP-04 | — | Pending |
| PROP-05 | — | Pending |
| PROP-06 | — | Pending |
| PROP-07 | — | Pending |

**Coverage:**
- v1.1 requirements: 21 total
- Mapped to phases: 0 (aguardando roadmap)
- Unmapped: 21 ⚠️

---
*Requirements defined: 2026-04-03*
*Last updated: 2026-04-03 — definição inicial v1.1*
