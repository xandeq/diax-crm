# Roadmap — DIAX CRM

## Milestones

- ✅ **v1.0 Base** — Plataforma completa em produção (shipped antes de 2026-04-03)
- ⚠️ **v1.1 Produtividade Pessoal** — Superado (planejado 2026-04-03, nunca executado pelo GSD; código evoluiu via sprints)
- ⏸️ **v1.2 Agentes de IA** — Pausado em paralelo desde 2026-05-29 (Phase 02, 2/4 plans); Phases 3-6 definidas, não iniciadas. Não abandonado — retomar com `/gsd:execute-phase 2`.
- ✅ **v1.3 Pipeline de Aquisição** — Phases 7-8 (shipped 2026-09-08) — [arquivo](milestones/v1.3-ROADMAP.md)
- 🟢 **v1.4 Funil de Outreach — Cadência e Qualidade** — Phases 9-14 (roadmap ativo, ver seção abaixo)

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

## v1.4 — Funil de Outreach — Cadência e Qualidade

**Status:** 🟢 Roadmap ativo (criado 2026-09-19). Numeração de fases continua de onde v1.3 parou
(Phase 8) — v1.4 começa em **Phase 9**. Roda em paralelo ao v1.2 pausado, sem tocar nos arquivos de
`src/Diax.Domain/Agents/*` nem correlatos.

**Milestone Goal:** Profissionalizar o funil de outreach de ponta a ponta — limpar/pontuar o
backlog que antecede o v1.3, dar cadência real ao follow-up (email+WhatsApp), tornar "Qualified" um
critério objetivo e automático, consolidar o script sprawl de disparo, e tirar o pipeline da
dependência de um único notebook.

**Fora do v1.4:** EXTR-04 (Google Places API) — fica no backlog de v2.

**Coverage:** 14/14 requirements mapped

### Phases

- [x] **Phase 9: Backfill de Score do Backlog** — Recalcular LeadScore/Segment do backlog pré-v1.3 com o motor oficial C# (BFIL-01) (completed 2026-09-19)
- [ ] **Phase 10: Motor de Cadência Único + Pipeline Fora do Notebook** — Consolidar os 79+ scripts `fire_*.py` num motor único e alertar quando WAHA/n8n caem (SCRP-01, OBSV-01) — 5 plans, 4 waves, planejada 2026-09-19
- [ ] **Phase 11: Qualified Objetivo e Automático** — Critério explícito de avanço Contacted→Qualified aplicado automaticamente pelo `reply_watch.py` (QUAL-01, QUAL-02)
- [ ] **Phase 12: Escala de Email** — DMARC 100%, volume 300/dia com warmup, A/B de subject medido, leads webmail testados (EMAL-01..04)
- [ ] **Phase 13: Escala de WhatsApp** — Handoff confirmado, bot desvia pra humano, relatório semanal, escalar wa_es→wa_br (WHAT-01..04)
- [ ] **Phase 14: Dashboard de Conversão do Funil** — Conversão percentual por etapa do funil (DASH-01)

### Phase Details

### Phase 9: Backfill de Score do Backlog

**Goal:** O backlog de ~5.266 leads pré-v1.3 (que nunca passaram pelo `LeadScoringService`) tem
`LeadScore`/`Segment` recalculados pelo motor oficial em C#, substituindo o fit score improvisado em
Python (`outreach_priority_valid_leads.csv`) usado na auditoria manual.

**Depends on:** Nothing (fase independente, usa `LeadScoringService` já existente — não muda a
lógica de scoring, só a roda sobre dados antigos)

**Requirements:** BFIL-01

**Success Criteria** (what must be TRUE):
  1. Rodando o backfill sobre o backlog pré-v1.3, todo `Customer` relevante tem `LeadScore`/`Segment`
     gravado por `LeadScoringService.CalculateScore`/`SegmentForScore` — não pelo script Python de
     auditoria manual
  2. A classificação junk/suspect/valid já feita na sessão de auditoria (2026-09-17/19) é
     reaproveitada como sinal de entrada — leads já marcados `Inactive` (`mark_junk_inactive.py`,
     1.065 registros) não são repontuados como Hot/Warm
  3. O backfill é seguro de re-rodar — executar duas vezes não duplica nem corrompe o score de um
     lead já processado
  4. Uma amostra antes/depois mostra `Segment` coerente com o teto de fit 45 do
     `LeadScoringService` oficial (Phase 8 do v1.3), não o teto 25 do script ad-hoc

**Plans:** 2/2 plans complete
- [x] 09-01-PLAN.md — Teste travando a exclusão Inactive/Churned no nível da query EF real + script
  `backfill_lead_scores.py` que dispara o motor oficial e captura evidência (Wave 1, BFIL-01)
- [x] 09-02-PLAN.md — Disparar o backfill em produção, provar idempotência com duplo disparo e
  escrever `BACKFILL-EVIDENCE.md` com números antes/depois (Wave 2, BFIL-01, tem checkpoint)

---

### Phase 10: Motor de Cadência Único + Pipeline Fora do Notebook

**Goal:** O disparo de outreach roda por um motor único parametrizado — não mais 79+ scripts
`fire_*.py` soltos — e o pipeline completo (waves, vigia de respostas, n8n, WAHA) deixa de depender
só do notebook do usuário via Task Scheduler, o ponto único de falha atual.

**Depends on:** Nothing (pode rodar em paralelo à Phase 9; é pré-requisito de fato para a Phase 12,
já que escalar volume de email com segurança exige um motor real, não scripts copiados)

**Requirements:** SCRP-01, OBSV-01

**Success Criteria** (what must be TRUE):
  1. Um único script/motor parametrizado (nicho, wave, data como argumentos) dispara uma campanha de
     outreach — nenhuma campanha nova exige copiar um `fire_*.py` novo
  2. Os 79+ scripts `fire_*.py` em `docs/email-marketing/` têm caminho de migração documentado
     (arquivados ou redirecionados ao motor único)
  3. Pelo menos uma etapa crítica do pipeline (waves, vigia de respostas, n8n ou WAHA) roda sem
     exigir que o notebook do usuário esteja ligado
  4. Uma falha ou desligamento do notebook não interrompe o pipeline em silêncio — existe log/alerta
     visível de que uma etapa parou

**Nota de planejamento (2026-09-19, decisões travadas em `10-CONTEXT.md`):** o critério 3 foi
**substituído** pelo usuário na discuss-phase. D-04: nada migra de máquina nesta fase — WAHA, n8n e
Task Scheduler continuam no notebook; a entrega é **alerta** quando eles caem, não eliminação do
ponto único de falha. Migrar para VPS foi para Deferred Ideas. D-05 também exclui explicitamente
alertar atraso do Task Scheduler e "rodou mas enviou 0 emails". Verificar a fase contra 1, 2 e 4
mais a nota de D-04/D-05 — não contra a leitura literal do critério 3.

Descoberta da pesquisa que redefiniu o SCRP-01: `daily_waves.py` **já é** o motor único
parametrizado (roda seg-sex 08:40 BRT com FUP1/2/3, circuit breaker e rotação de 20 nichos). O
trabalho é extrair dele a dependência dos scripts `fire_*` (D-01/D-02) — não reescrever motor.

**Plans:** 2/5 plans executed
- [x] 10-01-PLAN.md — Rede de segurança: `waves_smoke.py` (5 checks offline) + baseline congelado +
  doc de migração com inventário real (Wave 1, SCRP-01)
- [ ] 10-02-PLAN.md — Extrair `waves_core.py` (infra) + `waves_copy.py` (template/copy) e religar
  `daily_waves.py` (Wave 2, SCRP-01)
- [ ] 10-03-PLAN.md — Religar os 7 dependentes vivos, com `import_extrator_bridge.py` isolado e
  verificado por dry-run próprio (Wave 3, SCRP-01)
- [ ] 10-04-PLAN.md — Arquivar os 48 `fire_*.py` em `archive/` + fechar o doc de migração
  (Wave 4, SCRP-01, checkpoint bloqueante antes do move)
- [x] 10-05-PLAN.md — Alerta Telegram por transição de estado nos watchdogs existentes de WAHA/n8n
  + registrar a task `waha-watchdog` que não existia (Wave 1, OBSV-01, tem checkpoint)

---

### Phase 11: Qualified Objetivo e Automático

**Goal:** A transição Contacted→Qualified deixa de ser implícita/manual — vira um critério explícito
e documentado, aplicado automaticamente pelo `reply_watch.py`.

**Depends on:** Phase 10 (pipeline confiável e motor único tornam a automação de QUAL-02 segura de
rodar fora do notebook)

**Nota de planejamento:** `docs/email-marketing/reply_watch.py` está com mudanças não commitadas
(trabalho em andamento não relacionado a este milestone) no momento em que este roadmap foi criado.
O plano desta fase deve partir do estado atual do arquivo e incorporar a lógica de QUAL-02 sem
descartar esse trabalho em andamento — nunca assumir que o arquivo está limpo.

**Requirements:** QUAL-01, QUAL-02

**Success Criteria** (what must be TRUE):
  1. Existe critério explícito e documentado (ex.: resposta de email/WhatsApp que não seja bounce
     nem opt-out automático) definindo quando um Contacted vira Qualified
  2. `reply_watch.py`, preservando o trabalho em andamento não relacionado já presente no arquivo,
     passa a aplicar esse critério
  3. Um lead Contacted que responde conforme o critério tem o status avançado para Qualified no CRM
     automaticamente, sem edição manual
  4. Um lead que não atinge o critério (resposta automática, bounce, opt-out) permanece em Contacted
     — o critério não gera falso positivo

**Plans:** TBD

---

### Phase 12: Escala de Email

**Goal:** O canal de email escala volume com segurança de reputação (DMARC 100%, 300/dia com
warmup) e passa a medir o que já testa hoje sem leitura (A/B de subject, leads webmail).

**Depends on:** Phase 10 (motor único necessário para escalar volume com segurança — script ad-hoc
não tem warmup/controle de taxa confiável)

**Requirements:** EMAL-01, EMAL-02, EMAL-03, EMAL-04

**Success Criteria** (what must be TRUE):
  1. Política DMARC do domínio está em `pct=100` (verificável via consulta DNS TXT `_dmarc`)
  2. Volume diário de envio sobe de 87 para 300/dia seguindo uma curva de warmup gradual documentada
     — não um salto direto
  3. O resultado do teste A/B de subject (variant A vs B) é medido (taxa de abertura/resposta por
     variante) e aparece em relatório consultável — hoje só alterna, ninguém lê
  4. Os 560 leads "webmail" (gmail/hotmail comercial) são disparados com cap de volume próprio e tag
     distinta dos demais, permitindo medir a taxa de resposta separadamente

**Plans:** TBD

---

### Phase 13: Escala de WhatsApp

**Goal:** O canal de WhatsApp tem o handoff n8n→WAHA confirmado em execução, desvia lead de
prospecção do bot pra humano, reporta métricas semanais, e escala da base validada (`wa_es`) para a
maior (`wa_br`).

**Depends on:** Nothing além da infra já existente (trilha independente da Phase 12, pode rodar em
paralelo)

**Requirements:** WHAT-01, WHAT-02, WHAT-03, WHAT-04

**Success Criteria** (what must be TRUE):
  1. O workflow `n8n-wa-outreach-1a1.json` está confirmado em execução real na sessão que opera o
     WAHA local — não só criado, rodando de fato
  2. Quando um lead de prospecção manda mensagem, o bot inbound de IA (`8nKTlX4Y7Hy1EUEn`) desvia a
     conversa pra atendimento humano em vez de responder automaticamente
  3. Um relatório semanal do canal WhatsApp mostra enviados/respondidos/opt-out, consultável sem
     rodar script manual
  4. Após validar a taxa de resposta em `wa_es` (480 leads) contra um critério objetivo documentado,
     o disparo escala para `wa_br` (1138 leads)

**Plans:** TBD

---

### Phase 14: Dashboard de Conversão do Funil

**Goal:** O usuário enxerga, num dashboard, a conversão percentual por etapa do funil
(Lead→Contacted→Qualified→Customer) — não só volume bruto.

**Depends on:** Phase 11 (o sinal de Qualified precisa ser automático e confiável antes de virar
métrica de dashboard — reportar um critério manual/inconsistente produziria número enganoso)

**Requirements:** DASH-01

**Success Criteria** (what must be TRUE):
  1. Dashboard exibe contagem e percentual de conversão em cada etapa: Lead→Contacted,
     Contacted→Qualified, Qualified→Customer
  2. Os números refletem o `Status` real do `Customer`, usando o critério automático QUAL-01/QUAL-02
     — não uma contagem manual paralela
  3. Usuário pode filtrar o funil por período (ex.: última semana, último mês)
  4. Um gargalo numa etapa específica (ex.: Contacted→Qualified baixo) é identificável visualmente,
     sem precisar exportar dados pra planilha

**Plans:** TBD

---

### Progress (v1.4)

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 9. Backfill de Score do Backlog | 2/2 | Complete    | 2026-09-19 |
| 10. Motor de Cadência Único + Pipeline Fora do Notebook | 2/5 | In Progress|  |
| 11. Qualified Objetivo e Automático | 0/? | Not started | — |
| 12. Escala de Email | 0/? | Not started | — |
| 13. Escala de WhatsApp | 0/? | Not started | — |
| 14. Dashboard de Conversão do Funil | 0/? | Not started | — |

### Coverage Map (v1.4)

| Requirement | Phase | Status |
|-------------|-------|--------|
| BFIL-01 | Phase 9 | Complete |
| SCRP-01 | Phase 10 | Pending |
| OBSV-01 | Phase 10 | Complete |
| QUAL-01 | Phase 11 | Pending |
| QUAL-02 | Phase 11 | Pending |
| EMAL-01 | Phase 12 | Pending |
| EMAL-02 | Phase 12 | Pending |
| EMAL-03 | Phase 12 | Pending |
| EMAL-04 | Phase 12 | Pending |
| WHAT-01 | Phase 13 | Pending |
| WHAT-02 | Phase 13 | Pending |
| WHAT-03 | Phase 13 | Pending |
| WHAT-04 | Phase 13 | Pending |
| DASH-01 | Phase 14 | Pending |

**Total mapped: 14/14**

*v1.4 roadmap created: 2026-09-19 — Funil de Outreach — Cadência e Qualidade (6 phases, 14
requirements). Numeração de fases continua de v1.3 (Phase 8) → v1.4 começa em Phase 9. Nenhum plano
criado ainda — próximo passo: `/gsd:plan-phase 9`.*

---
