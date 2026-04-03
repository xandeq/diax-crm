# Roadmap: DIAX CRM — v1.1 Produtividade Pessoal

## Milestones

- ✅ **v1.0 Base** — Plataforma completa em produção (shipped antes de 2026-04-03)
- 🚧 **v1.1 Produtividade Pessoal** — Phases 1-5 (in progress)

## Phases

<details>
<summary>✅ v1.0 Base — SHIPPED (pre-2026-04-03)</summary>

Auth, CRM, financeiro, e-mail, WhatsApp, IA, Facebook Ads, Blog, Agenda, Admin, Dashboard — todos em produção.

</details>

### 🚧 v1.1 Produtividade Pessoal

**Milestone Goal:** Transformar o CRM de repositório de dados em ferramenta de ação diária — tarefas pessoais, pipeline visual, propostas com PDF e Morning Briefing unificado.

- [ ] **Phase 1: Tarefas** - Módulo de tarefas avulsas com CRUD completo, prioridade e filtros
- [ ] **Phase 2: Pipeline Kanban** - Visualização kanban dos leads com drag-and-drop por etapa
- [ ] **Phase 3: Propostas Core** - Entidades de proposta e template, CRUD, vínculo com cliente
- [ ] **Phase 4: Propostas Avancadas** - Geração por IA e exportação PDF
- [ ] **Phase 5: Morning Briefing** - Dashboard unificado com agenda, leads, tarefas e financeiro

## Phase Details

### Phase 1: Tarefas
**Goal**: Usuário pode gerenciar suas tarefas pessoais com prazo, prioridade e status
**Depends on**: Nothing (entidade nova, sem dependências)
**Requirements**: TASK-01, TASK-02, TASK-03, TASK-04, TASK-05
**Success Criteria** (what must be TRUE):
  1. Usuário pode criar uma tarefa com título, descrição opcional, prazo e prioridade (baixa/média/alta)
  2. Usuário pode marcar uma tarefa como concluída e revertê-la para pendente
  3. Usuário pode editar e excluir qualquer tarefa existente
  4. Usuário pode filtrar a lista de tarefas por status (pendente/concluída) e por prioridade
  5. Contagem de tarefas pendentes do dia aparece visível no header ou no dashboard
**Plans**: TBD

### Phase 2: Pipeline Kanban
**Goal**: Usuário vê todos os leads organizados visualmente por etapa e pode mover entre etapas com drag-and-drop
**Depends on**: Phase 1 (infrastructure validada; dados de Customer já existem em v1.0)
**Requirements**: PIPE-01, PIPE-02, PIPE-03, PIPE-04, PIPE-05
**Success Criteria** (what must be TRUE):
  1. Usuário acessa uma página Kanban com colunas: Lead, Contatado, Qualificado, Negociando, Cliente
  2. Cada card exibe nome, empresa, segmento (Hot/Warm/Cold) e data da última interação
  3. Usuário arrasta um card de uma coluna para outra e a etapa do lead é salva na API
  4. Usuário clica em qualquer card e é redirecionado para o detalhe do lead existente
  5. Cada coluna exibe a contagem de leads que ela contém
**Plans**: TBD

### Phase 3: Propostas Core
**Goal**: Usuário pode criar e gerenciar propostas comerciais completas com templates e vínculo a clientes
**Depends on**: Phase 1 (infra validada); Customer entity de v1.0
**Requirements**: PROP-01, PROP-02, PROP-03, PROP-04, PROP-07
**Success Criteria** (what must be TRUE):
  1. Usuário pode criar um template de proposta com nome e itens de serviço padrão (descrição, valor unitário)
  2. Usuário pode criar uma proposta a partir de um template, preenchendo dados do cliente
  3. Proposta gerada contém dados do cliente, itens de serviço, total calculado, data de validade e linha de assinatura
  4. Usuário pode editar os itens (adicionar, remover, alterar valor/quantidade) antes de finalizar
  5. Usuário pode vincular uma proposta a um lead/cliente existente no CRM
**Plans**: TBD

### Phase 4: Propostas Avancadas
**Goal**: Usuário pode gerar o texto da proposta com IA e exportar como PDF profissional
**Depends on**: Phase 3 (entidades de proposta existem e estão populadas)
**Requirements**: PROP-05, PROP-06
**Success Criteria** (what must be TRUE):
  1. Usuário clica em "Gerar com IA" numa proposta e recebe um texto comercial completo baseado no cliente e nos serviços selecionados
  2. Usuário clica em "Exportar PDF" e faz download de um arquivo PDF formatado com todos os dados da proposta
**Plans**: TBD

### Phase 5: Morning Briefing
**Goal**: Usuário abre o CRM e vê imediatamente todos os dados relevantes do dia em um único lugar
**Depends on**: Phase 1 (tarefas), Phase 2 (leads/pipeline), v1.0 agenda e financeiro
**Requirements**: BRIEF-01, BRIEF-02, BRIEF-03, BRIEF-04
**Success Criteria** (what must be TRUE):
  1. Dashboard exibe os compromissos de hoje da agenda existente ao acessar o CRM
  2. Dashboard lista leads Warm ou Hot sem contato registrado nos últimos 7 dias
  3. Dashboard lista tarefas com prazo igual a hoje ou vencidas (usando o módulo de Phase 1)
  4. Dashboard exibe saldo total das contas e o resumo de receitas e despesas do mês corrente
**Plans**: TBD

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Tarefas | v1.1 | 0/? | Not started | - |
| 2. Pipeline Kanban | v1.1 | 0/? | Not started | - |
| 3. Propostas Core | v1.1 | 0/? | Not started | - |
| 4. Propostas Avancadas | v1.1 | 0/? | Not started | - |
| 5. Morning Briefing | v1.1 | 0/? | Not started | - |

---
*Roadmap created: 2026-04-03 — v1.1 Produtividade Pessoal, 5 phases, 21 requirements*
