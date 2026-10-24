# Roadmap — DIAX CRM

## Milestones

- ✅ **v1.0 Base** — Plataforma completa em produção (shipped antes de 2026-04-03)
- ⚠️ **v1.1 Produtividade Pessoal** — Superado (planejado 2026-04-03, nunca executado pelo GSD; código evoluiu via sprints)
- ⏸️ **v1.2 Agentes de IA** — Pausado em paralelo desde 2026-05-29 (Phase 02, 2/4 plans); Phases 3-6 definidas, não iniciadas. Não abandonado — retomar com `/gsd:execute-phase 2`.
- ✅ **v1.3 Pipeline de Aquisição** — Phases 7-8 (shipped 2026-09-08) — [arquivo](milestones/v1.3-ROADMAP.md)
- ✅ **v1.4 Funil de Outreach — Cadência e Qualidade** — Phases 9-14, 6/6 concluídas (shipped 2026-09-20) — [arquivo](milestones/v1.4-ROADMAP.md)
- 🟢 **v1.5 Fechar Venda** — Phases 15-20 (current) — catálogo de pacotes com preço fixo, proposta gerada a partir de pacote, follow-up automático, painel de receita e bot vendedor em modo sombra

---

<details>
<summary>✅ v1.3 — Pipeline de Aquisição (Phases 7-8) — SHIPPED 2026-09-08</summary>

**Entregue:** qualidade de dado na entrada (MX check no worker .NET, filtro de domínio-lixo,
contadores de rejeição consultáveis por período, classificação site-próprio-vs-diretório) e
rastreabilidade no import (dedup por `Customer.ExternalId` com fallback e-mail/telefone, paridade
de dedup para `source=Scraping`, `lead_score` e segmento calculados no momento do import).

- [x] **Phase 7: Extração — Qualidade na Entrada** (7/7 planos) — concluída 2026-09-06, deployada no PR #99
- [x] **Phase 8: Import — Dedup e Score em Tempo Real** (4/4 planos) — concluída 2026-09-08, deployada no PR #111

Requisitos EXTR-01..03 e IMPT-01..03 — 6/6 validados.
Suíte de testes: 833 → 891. Verificação de fase: 4/4 critérios em ambas.

Detalhes completos: [`milestones/v1.3-ROADMAP.md`](milestones/v1.3-ROADMAP.md) ·
requisitos: [`milestones/v1.3-REQUIREMENTS.md`](milestones/v1.3-REQUIREMENTS.md)

**Débito técnico conhecido:** o dry-run de `/customers/import` deduplica em memória só por e-mail e
telefone, então uma prévia com o mesmo `ExternalId` repetido reporta sucesso onde o import real
pularia a linha. A dedup real está correta; é divergência entre prévia e execução.

</details>

---

<details>
<summary>✅ v1.4 — Funil de Outreach — Cadência e Qualidade (Phases 9-14) — SHIPPED 2026-09-20</summary>

**Entregue:** backlog pré-v1.3 recalculado com o motor oficial de score, consolidação dos 79+
scripts `fire_*.py` num motor único (`waves_core.py`) com o pipeline tirado da dependência do
notebook (watchdogs + Scheduled Tasks), critério objetivo e automático de avanço Contacted→Qualified
(`reply_watch.py`), ferramentas de escala para email (DMARC/warmup/A-B/webmail) e WhatsApp
(handoff/bot de desvio/relatório semanal/escalonamento wa_es→wa_br), e dashboard de conversão do
funil verificado em produção.

- [x] **Phase 9: Backfill de Score do Backlog** (2/2 planos) — concluída 2026-09-19
- [x] **Phase 10: Motor de Cadência Único + Pipeline Fora do Notebook** (5/5 planos) — concluída 2026-09-19
- [x] **Phase 11: Qualified Objetivo e Automático** (1/1 plano) — concluída 2026-09-20
- [x] **Phase 12: Escala de Email** (6/6 planos) — concluída 2026-09-20
- [x] **Phase 13: Escala de WhatsApp** (4/4 planos) — concluída 2026-09-20
- [x] **Phase 14: Dashboard de Conversão do Funil** (1/1 plano) — concluída 2026-09-20

Requisitos BFIL-01, SCRP-01, OBSV-01, QUAL-01/02, WHAT-01..04, DASH-01 — 11/14 `Complete`.
EMAL-01/02/04 — 3/14 `Partial` por decisão deliberada de segurança/calendário (bounce rate acima do
limiar de reversão travado pelo próprio usuário; execução real pendente de confirmação agendada),
não por falha de implementação — ver Traceability no arquivo.

Detalhes completos: [`milestones/v1.4-ROADMAP.md`](milestones/v1.4-ROADMAP.md) ·
requisitos: [`milestones/v1.4-REQUIREMENTS.md`](milestones/v1.4-REQUIREMENTS.md)

**Observação de workflow (não é débito técnico do produto):** este milestone foi executado e
documentado inteiramente na branch `chore/email-automation-versioned` (pushada para `origin`), que
ainda não foi mergeada em `main`/`origin/main` no momento em que o v1.5 começou a ser planejado —
`main` segue 209 commits atrás. O trabalho está pronto e com GSD completo, mas **não está em
produção** (deploy dispara só em push para `main`, por `CLAUDE.md`). Confirmar com o usuário a ordem
de merge antes de tratar v1.4 como "em produção".

</details>

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

## v1.2 — Agentes de IA (paused)

**Status:** ⏸️ Pausado desde 2026-05-29, após Phase 2 Wave 1 (2 de 4 plans). Não abandonado — retomar
com `/gsd:execute-phase 2` quando decidido. Motivo: sessão concorrente no mesmo repo. Commits locais
em `main`, **não pushados**; migration `20260529134701_AddAgentFoundation` já aplicada em produção —
coordenar ordem antes de gerar novas migrations. Detalhe completo em `STATE.md` seção "v1.2 —
Pausado". v1.3/v1.4 não tocam nos arquivos de `src/Diax.Domain/Agents/*` nem correlatos enquanto v1.2
estiver pausado.

**Milestone Goal:** Transformar o CRM de repositório de dados em parceiro de execução por IA — três agentes (Comercial, Suporte, Pessoal) que conversam usando os dados reais do CRM e executam ações sob confirmação, reaproveitando a infra de IA existente sem quebrar nada.

**Coverage:** 24/24 requirements mapped

### Phases

- [ ] **Phase 2: Fundação de Agentes** — AgentOrchestrator, framework de tools com confirmação, persistência de conversas (ORCH-01..05)
- [ ] **Phase 3: Agente Comercial** — Validar e completar o agente já parcialmente construído; tools de outreach draft e atualização de lead (CMRC-01..06)
- [ ] **Phase 4: Agente de Suporte** — Contexto do histórico do cliente, sugestão de resposta, resumo e abertura de ticket (SUP-01..04)
- [ ] **Phase 5: Agente Pessoal** — Agenda e finanças reais, criação de compromisso via ação confirmada (PERS-01..04)
- [ ] **Phase 6: UI /agentes** — Página unificada com seletor de agente, streaming chat, contexto anexado, botões de confirmação, design /impeccable (AGUI-01..05)

### Phase Details

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

**Plans:** 2/4 plans executed
- [x] 02-01-PLAN.md — Persistence foundation: AgentPendingAction entity + repo, AgentType nullable on AiConversation, migrations (Wave 1, ORCH-04/05)
- [x] 02-02-PLAN.md — LLM tool-use: CompleteWithToolsAsync on IAnthropicChatClient (non-breaking, Wave 1, ORCH-02)
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

### Progress (v1.2)

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 2. Fundação de Agentes | 2/4 | In Progress|  |
| 3. Agente Comercial | 0/? | Not started | — |
| 4. Agente de Suporte | 0/? | Not started | — |
| 5. Agente Pessoal | 0/? | Not started | — |
| 6. UI /agentes | 0/? | Not started | — |

### Coverage Map (v1.2)

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

*v1.2 roadmap last updated: 2026-05-28 — Agentes de IA (5 phases, 24 requirements). Phase 2 planned: 4 plans, 3 waves. Paused 2026-05-29 (see STATE.md "v1.2 — Pausado").*

---


## v1.5 — Fechar Venda (current)

**Status:** 🟢 Iniciado 2026-10-23. Design e plano de implementação já aprovados pelo usuário antes
do roadmap (fluxo diferente das milestones anteriores — ver nota abaixo). Numeração de fases continua
de v1.4 (Phase 14) — v1.5 começa em **Phase 15**.

**Origem:** `docs/plans/2026-10-23-fechar-venda-design.md` (design aprovado 23/10/2026) e
`docs/plans/2026-10-23-fechar-venda.md` (plano de implementação, task-by-task, TDD). Este roadmap
**mapeia** esse plano em fases GSD — não duplica o conteúdo. Cada fase abaixo aponta para os números
de Task do plano em vez de copiar código/instruções.

**Milestone Goal:** O CRM passa a acompanhar a venda depois do primeiro contato — lead que respondeu
ganha uma proposta de pacote com preço fixo (Rascunho → revisão do dono → envio), follow-up
automático quando a proposta é esquecida, visibilidade de receita em proposta aberta, e o bot do
WhatsApp passa a conduzir a conversa até esse rascunho (primeiro em modo sombra, nunca enviando nada
até aprovação humana).

**Contexto de origem:** 45 dias de outreach (738 e-mails, ~0,1% resposta humana; 553 WhatsApps, ~34%
resposta incluindo autoresposta) não geraram nenhuma proposta desde 03/07 — pipeline com R$0. O bot
do WhatsApp (`n8n-local/workflows/waha-bot-atendente.json`) responde sem histórico, sem saber quem é
o lead e sem objetivo; em pergunta de preço/prazo diz "o Alexandre responde em seguida" e nada
acontece no CRM. Decisões do usuário: gargalo = fechar venda; passo que fecha = proposta direto (sem
call); preço por pacotes fixos; abordagem em 2 fatias (CRM primeiro, bot depois, sempre com revisão
humana antes de qualquer envio).

**Reaproveitado (sem reescrever):** `Proposal` (Draft→Sent→Accepted→Paid/Cancelled, link público,
aceite público, `mark-paid`, `pix_key`, `view_count`); pipeline Kanban (`Customer.Status` é a própria
oportunidade); padrão de worker diário idempotente com `job_runs` (`CommercialBriefingWorker`);
evento de integração `POST /api/v1/integrations/whatsapp-event` com `X-Integration-Key`. As tabelas
legadas `Opportunities`/`Sales`/`Products`/`Leads` (sem código, nunca usadas) ficam como estão.

**Fora do v1.5:** gateway de pagamento, contrato/assinatura, itens adicionais por proposta,
ativação real do bot (fica para plano separado, só depois da revisão humana da sombra).

**Nota de risco de produção (vale para as Phases 15-19):** `Program.cs` chama
`db.Database.Migrate()` no startup — merge na `main` é deploy **e** migration em produção ao mesmo
tempo. Toda migration desta milestone deve ser lida linha a linha (só `CreateTable`/`AddColumn`/
`CreateIndex`) antes do merge, e merge só acontece com ok explícito do usuário.

**Requirements:** PKG-01, PKG-02, PKG-03, PKG-04, PKG-05, FUP-01, FUP-02, REV-01, BOT-01, BOT-02, BOT-03

### Phases

- [~] **Phase 15: Catálogo de Pacotes** — entidade `ServicePackage`, campos novos em `Proposal`, schema+migration, CRUD via API (PKG-01, PKG-02) — **em execução por superpowers-dev** (worktree `_worktrees/diax-pr-a`, branch `feat/service-packages`, plano Tasks 1-4 = PR-a)
- [ ] **Phase 16: Proposta a Partir de Pacote** — API de proposta usando pacote, tela de pacotes em crm-web, seletor de pacote no pipeline (PKG-03, PKG-04, PKG-05) — plano Tasks 5-7 = PR-b
- [ ] **Phase 17: Follow-up de Proposta** — regra de etapas (não vista/vista sem aceite/vencida) + worker diário idempotente (FUP-01, FUP-02) — plano Tasks 8-9 = PR-c
- [ ] **Phase 18: Painel de Receita** — valor em proposta aberta no dashboard comercial (REV-01) — plano Task 10 = PR-d
- [ ] **Phase 19: Evento do Bot + Catálogo** — `proposal_draft` cria rascunho a partir da decisão do bot; endpoint de catálogo somente leitura para o bot (BOT-01) — plano Task 11 = PR-e
- [ ] **Phase 20: Bot Vendedor em Modo Sombra** — decide resposta/pacote/resumo sem nunca enviar nada; conectado ao `outreach-watch` sem alterar o comportamento atual (BOT-02, BOT-03) — plano Tasks 12-13, **fora do repo** (`n8n-local`, sem git), fora do ciclo normal de PR

### Phase Details

### Phase 15: Catálogo de Pacotes

**Goal:** O catálogo de pacotes de serviço (preço fixo) existe, é gerenciável via API, e `Proposal`
está pronta para linkar a um pacote e rastrear follow-up — fundação sem qualquer impacto visível
ainda no app em produção.

**Depends on:** Nothing (reaproveita `Proposal`/pipeline existentes; não altera comportamento atual)

**Requirements:** PKG-01, PKG-02

**Success Criteria** (what must be TRUE):
  1. `POST/PUT/GET /api/v1/service-packages` cria, atualiza e lista pacotes do usuário autenticado
     (nome, descrição curta, escopo, preço, prazo, ativo, ordem), rejeitando nome vazio/preço ≤0/prazo
     negativo
  2. `ServicePackage.ToProposalDescription()` gera texto com o escopo em bullets e o prazo por
     extenso, pronto para popular uma proposta sem transformação adicional
  3. `Proposal` ganha `PackageId` (opcional), `Origin` (`Manual`/`Bot`) e `LastFollowUpStage`, com
     `RegisterFollowUpStage` só avançando (nunca retrocede, nunca repete a mesma etapa)
  4. Migration única (`service_packages` + 3 colunas em `proposals`) sem `DropTable`/`DropColumn`/
     `AlterColumn` de nenhuma outra tabela — lida linha a linha antes do commit
  5. Suíte .NET cobre entidade/serviço/repositório; `dotnet build -c Release` 0 erros; nenhum teste
     pré-existente quebra
  6. PR-a aberto com CI 6/6; merge pendente de ok explícito do usuário (migration só roda em
     produção depois do merge, via auto-migrate do startup)

**Plans:** plano externo `docs/plans/2026-10-23-fechar-venda.md` Tasks 1-4 (não há PLAN.md GSD —
ver `15-CONTEXT.md` nesta pasta). Verificação: `15-VERIFICATION.md`.

---

### Phase 16: Proposta a Partir de Pacote

**Goal:** O usuário gera uma proposta a partir de um pacote direto do pipeline, sem digitar
preço/escopo à mão — nasce Rascunho, revisável antes de enviar.

**Depends on:** Phase 15 (catálogo de pacotes e campos novos de `Proposal`)

**Requirements:** PKG-03, PKG-04, PKG-05

**Success Criteria** (what must be TRUE):
  1. `POST /proposals` com `packageId` preenche título/descrição/valor a partir do pacote quando os
     campos manuais vêm vazios/zero; valor manual informado sobrescreve o do pacote
  2. `packageId` de outro usuário ou inexistente retorna falha (`NotFound`/`Validation`) e nada é
     salvo
  3. Tela `/propostas/pacotes` lista, cria e edita pacotes com o mesmo padrão visual de `/propostas`
  4. Modal "Gerar proposta" do pipeline tem seletor de pacote; ao escolher, preenche título/
     descrição/valor automaticamente e usa validade de **7 dias** (não mais 15)
  5. `npx vitest run` e `npm run build` passam; nenhuma proposta real é criada em produção como parte
     do teste manual

**Plans:** plano externo Tasks 5-7 (PR-b). Sem PLAN.md GSD — mapeado neste ROADMAP.

---

### Phase 17: Follow-up de Proposta

**Goal:** Proposta enviada e esquecida gera automaticamente uma tarefa de follow-up, uma vez por
etapa, sem duplicar em restart do worker.

**Depends on:** Phase 16 (precisa de propostas reais passando por `Sent`)

**Requirements:** FUP-01, FUP-02

**Success Criteria** (what must be TRUE):
  1. `ProposalFollowUpService.StageFor` classifica: Draft/Accepted/Paid/Cancelled → 0; Sent há <48h
     → 0; Sent há ≥48h sem visualização → 1 (não vista); Sent há ≥48h visualizada → 2 (vista sem
     aceite); vencida → 3
  2. Rodar a regra duas vezes sobre a mesma proposta no mesmo estágio cria exatamente **1** tarefa
     (idempotência via `RegisterFollowUpStage`)
  3. Worker diário (`ProposalFollowUpWorker`) segue o padrão `job_runs`/`DailyJobLedger` de
     `CommercialBriefingWorker`, registrado como hosted service em `DependencyInjection.cs`
  4. Suíte .NET completa passa; PR-c com CI 6/6; merge só com ok explícito do usuário

**Plans:** plano externo Tasks 8-9 (PR-c). Sem PLAN.md GSD — mapeado neste ROADMAP.

---

### Phase 18: Painel de Receita

**Goal:** O dono vê, sem somar nada manualmente, quanto R$ está em proposta aberta no dashboard
comercial.

**Depends on:** Phase 16 (proposta com valor e status reais fluindo)

**Requirements:** REV-01

**Success Criteria** (what must be TRUE):
  1. `SalesDashboardService.OpenValue` soma `Amount` de propostas `Sent` + `Accepted` (exclui
     `Draft`/`Paid`/`Cancelled`) — cobrado por teste unitário com os 4 status misturados
  2. Card "Em proposta aberta" aparece em `/dashboard-comercial` ao lado dos cards existentes, mesmo
     componente visual
  3. `dotnet test` + `npm run build` passam; PR-d com CI 6/6; merge só com ok do usuário

**Plans:** plano externo Task 10 (PR-d). Sem PLAN.md GSD — mapeado neste ROADMAP.

---

### Phase 19: Evento do Bot + Catálogo

**Goal:** O CRM sabe receber a decisão de um bot externo e transformá-la em proposta Rascunho
revisável, e expõe o catálogo somente leitura para quem vai tomar essa decisão — preparando o
terreno para a Phase 20 sem ativar nada ainda.

**Depends on:** Phase 15 (pacotes) e Phase 16 (`CreateAsync` de proposta reutilizável)

**Requirements:** BOT-01

**Success Criteria** (what must be TRUE):
  1. `whatsapp-event {event:'proposal_draft', customerId, packageId, resumo}` cria proposta Rascunho
     (`Origin=Bot`), move o cliente para `Negotiating` e cria tarefa urgente com o resumo truncado em
     256 caracteres (`TaskItem.Description` é `nvarchar(256)`)
  2. Chamar de novo para o mesmo cliente com proposta aberta **não** cria uma segunda — no máximo 1
     rascunho aberto por lead
  3. `packageId` inexistente/inativo/de outro usuário falha sem salvar nada
  4. `GET /integrations/service-packages` com `X-Integration-Key` válido retorna só pacotes ativos
     (id/nome/descrição/preço/prazo); chave inválida/ausente nega acesso, reaproveitando a mesma
     checagem de `whatsapp-event` (extraída para um método privado comum, não duplicada)
  5. Suíte .NET completa passa; PR-e com CI 6/6; merge só com ok explícito do usuário

**Plans:** plano externo Task 11 (PR-e). Sem PLAN.md GSD — mapeado neste ROADMAP.

---

### Phase 20: Bot Vendedor em Modo Sombra

**Goal:** Existe uma prova registrada de como o bot decidiria a conversa até o rascunho — sem nunca
enviar nada nem tocar o CRM — pronta para o dono revisar antes de qualquer ativação real.

**Depends on:** Phase 19 (catálogo exposto + evento `proposal_draft` prontos, mesmo que a sombra
ainda não os chame)

**Requirements:** BOT-02, BOT-03

**Success Criteria** (what must be TRUE):
  1. `validar()` bloqueia qualquer preço no `reply` que não seja o `price` de um item do catálogo
  2. Pacote inexistente nunca fecha `pronto_para_proposta=True`
  3. Mensagem manual do dono no chat nas últimas 24h silencia o bot naquele chat (`dono_assumiu`);
     depois de 24h, volta a decidir
  4. 6 ou mais respostas do bot no histórico aciona `estourou_limite`
  5. Ligar a sombra no laço do `outreach-watch.py` (dentro de `try/except` que só loga) não altera o
     comportamento de produção atual — reinjeção no bot atual intacta, `outreach-audit.log` igual
  6. `bot_vendas.py --sombra` nunca chama `sendText` nem qualquer endpoint do CRM — só grava
     `logs/bot-sombra.jsonl`
  7. Backup (`*.bak-<data>`) feito antes de editar qualquer script já existente (`outreach-watch.py`)

  **Ativação real (enviar resposta + disparar `proposal_draft`) é um plano separado, fora desta
  fase — só depois do dono revisar 10-20 linhas do `.jsonl`.**

**Plans:** plano externo Tasks 12-13, **fora do repo git** (`D:\claude-code\n8n-local\`). Sem
PLAN.md GSD, sem commit — mapeado neste ROADMAP só para rastreabilidade do milestone.

---

### Progress (v1.5)

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 15. Catálogo de Pacotes | 0/4 tasks | In Progress (superpowers-dev) | — |
| 16. Proposta a Partir de Pacote | 0/3 tasks | Not started | — |
| 17. Follow-up de Proposta | 0/2 tasks | Not started | — |
| 18. Painel de Receita | 0/1 task | Not started | — |
| 19. Evento do Bot + Catálogo | 0/1 task | Not started | — |
| 20. Bot Vendedor em Modo Sombra | 0/2 tasks | Not started | — |

### Coverage Map (v1.5)

| Requirement | Phase | Status |
|-------------|-------|--------|
| PKG-01 | Phase 15 | Pending |
| PKG-02 | Phase 15 | Pending |
| PKG-03 | Phase 16 | Pending |
| PKG-04 | Phase 16 | Pending |
| PKG-05 | Phase 16 | Pending |
| FUP-01 | Phase 17 | Pending |
| FUP-02 | Phase 17 | Pending |
| REV-01 | Phase 18 | Pending |
| BOT-01 | Phase 19 | Pending |
| BOT-02 | Phase 20 | Pending |
| BOT-03 | Phase 20 | Pending |

**Total mapped: 11/11**

---
*Roadmap criado 2026-10-24 — v1.5 Fechar Venda (6 fases, 11 requisitos, Phases 15-20). Numeração de
fases continua de v1.4 (Phase 14) → v1.5 começa em Phase 15. Mapeado a partir de
`docs/plans/2026-10-23-fechar-venda-design.md` e `docs/plans/2026-10-23-fechar-venda.md` (já
aprovados pelo usuário antes deste roadmap existir) — fases apontam para as Tasks do plano em vez de
duplicar código. Phase 15 em execução por superpowers-dev no worktree `_worktrees/diax-pr-a`
(branch `feat/service-packages`) no momento da criação deste roadmap.*
