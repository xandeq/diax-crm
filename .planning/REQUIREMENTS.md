# Requirements: DIAX CRM — Milestone v1.5

**Defined:** 2026-10-24
**Core Value:** Centralizar todas as operações de negócio (leads, finanças, comunicação, IA) em um único sistema pessoal, eliminando ferramentas externas pagas

## v1 Requirements

Requirements para o milestone v1.5 "Fechar Venda". Cada um mapeia para uma fase do roadmap.
Origem: `docs/plans/2026-10-23-fechar-venda-design.md` (design aprovado pelo usuário 23/10/2026) e
`docs/plans/2026-10-23-fechar-venda.md` (plano de implementação task-by-task).

### Pacotes de Serviço

- [ ] **PKG-01**: Catálogo `service_packages` (nome, descrição curta, escopo, preço fixo, prazo de
  entrega, ativo, ordem) com CRUD via API (`/api/v1/service-packages`), auditoria padrão
- [ ] **PKG-02**: `Proposal` ganha `PackageId` (opcional), `Origin` (`Manual`/`Bot`) e
  `LastFollowUpStage`, preparando o terreno para as Phases 16/17/19 sem alterar comportamento atual
- [ ] **PKG-03**: Gerar proposta a partir de um pacote preenche automaticamente título, descrição
  (escopo em bullets + prazo) e valor; valor manual informado continua podendo sobrescrever o do
  pacote
- [ ] **PKG-04**: Tela `/propostas/pacotes` em crm-web para listar, criar e editar pacotes
- [ ] **PKG-05**: Seletor de pacote no modal "Gerar proposta" do pipeline, com validade padrão de
  7 dias (ajuste do design — era 15)

### Follow-up de Proposta

- [ ] **FUP-01**: Critério objetivo e idempotente de follow-up por etapa — enviada e não visualizada
  há 48h (reenviar/chamar no WhatsApp); visualizada e não aceita há 48h (follow-up); vencida
  (renovar/encerrar) — uma tarefa por proposta por etapa, nunca duplicada
- [ ] **FUP-02**: Worker diário (`job_runs`, padrão `CommercialBriefingWorker`/`DailyJobLedger`) roda
  a regra FUP-01 sobre as propostas abertas e cria as tarefas correspondentes

### Painel de Receita

- [ ] **REV-01**: Painel comercial mostra R$ em proposta aberta (enviada + aceita) somado
  corretamente, sem incluir Draft/Paid/Cancelled

### Bot Vendedor (WhatsApp)

- [ ] **BOT-01**: Evento de integração `proposal_draft` (`X-Integration-Key`) cria proposta
  Rascunho a partir de um pacote, move o cliente para `Negotiating` e cria tarefa urgente de revisão
  com o resumo; endpoint `GET /integrations/service-packages` expõe o catálogo ativo (somente
  leitura) para quem vai tomar essa decisão
- [ ] **BOT-02**: Bot vendedor roda em **modo sombra** — decide `{reply, intent, package_id,
  pronto_para_proposta, resumo}` a partir do histórico (WAHA) e do catálogo, nunca envia nada, nunca
  chama o CRM; trava preço fora do catálogo, pacote inexistente, mais de 6 respostas e mensagem
  manual do dono nas últimas 24h; grava toda decisão em log (`.jsonl`) para revisão humana
- [ ] **BOT-03**: Modo sombra conectado ao `outreach-watch.py` existente sem alterar o
  comportamento de produção atual do bot (reinjeção no bot atual intacta)

## v2 Requirements

Deferido — não bloqueia v1.5. Decisão explícita do design: **ativação real do bot** (enviar
resposta de fato e disparar `proposal_draft` de produção) só acontece depois do usuário revisar
10-20 conversas do log de sombra; é um plano separado, fora desta milestone.

- **BOT-04**: Ativar o bot vendedor em produção (enviar respostas reais, acionar `proposal_draft`
  de verdade) — depende da revisão humana do modo sombra (BOT-02/BOT-03)
- Gateway de pagamento, contrato/assinatura digital, itens adicionais por proposta — fora do v1.5
  por decisão de design (ver `docs/plans/2026-10-23-fechar-venda-design.md`, "Fora da v1")

## Out of Scope

| Feature | Reason |
|---------|--------|
| Gateway de pagamento | Fora da v1 por decisão de design — proposta aceita/paga permanece manual (`mark-paid`) |
| Contrato/assinatura digital | Fora da v1 por decisão de design |
| Itens adicionais por proposta | Fora da v1 por decisão de design — proposta é de pacote fechado |
| Remover tabelas legadas `Opportunities`/`Sales`/`Products`/`Leads` | Sem código, nunca usadas, mas remover é tarefa separada — não misturar com v1.5 |
| Retomar v1.2 (Agentes de IA) | Milestone paralelo, pausado, escopo próprio — não misturar com v1.5 |
| Ativação real do bot vendedor (BOT-04) | Depende de revisão humana do modo sombra desta milestone — plano separado |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| PKG-01 | Phase 15 — Catálogo de Pacotes | Pending — em execução por superpowers-dev (PR-a, plano Tasks 1-4) |
| PKG-02 | Phase 15 — Catálogo de Pacotes | Pending — em execução por superpowers-dev (PR-a, plano Tasks 1-4) |
| PKG-03 | Phase 16 — Proposta a Partir de Pacote | Pending (plano Task 5, PR-b) |
| PKG-04 | Phase 16 — Proposta a Partir de Pacote | Pending (plano Task 6, PR-b) |
| PKG-05 | Phase 16 — Proposta a Partir de Pacote | Pending (plano Task 7, PR-b) |
| FUP-01 | Phase 17 — Follow-up de Proposta | Pending (plano Task 8, PR-c) |
| FUP-02 | Phase 17 — Follow-up de Proposta | Pending (plano Task 9, PR-c) |
| REV-01 | Phase 18 — Painel de Receita | Pending (plano Task 10, PR-d) |
| BOT-01 | Phase 19 — Evento do Bot + Catálogo | Pending (plano Task 11, PR-e) |
| BOT-02 | Phase 20 — Bot Vendedor em Modo Sombra | Pending (plano Task 12, fora do repo) |
| BOT-03 | Phase 20 — Bot Vendedor em Modo Sombra | Pending (plano Task 13, fora do repo) |

**Total mapped: 11/11 — 100% coverage, no orphans.**

---
*Requirements defined: 2026-10-24 — v1.5 Fechar Venda. Numeração de fases continua de v1.4 (Phase
14) → v1.5 começa em Phase 15. Mapeado a partir do design/plano já aprovados pelo usuário em
23/10/2026 (fluxo inverso ao usual: design→plano vieram antes do roadmap GSD).*
