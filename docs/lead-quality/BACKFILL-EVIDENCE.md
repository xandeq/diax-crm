# Backfill de Score do Backlog — Evidência (BFIL-01, Phase 9)

**Executado:** 2026-09-19T12:30:32.747473+00:00 (1º disparo) / 2026-09-19T12:30:39.616096+00:00 (2º disparo, prova de idempotência)
**Motor:** LeadScoringService.RecomputeAllAsync (C#), via POST /api/v1/pipeline/recompute-scores
**NÃO usado:** OutreachService.SegmentLeadsAsync (motor legado 0-6 pontos) — fora de escopo

## Antes → Depois

| Métrica | Baseline | Após 1º disparo | Após 2º disparo |
|---|---|---|---|
| openLeadsTotal | 7242 | 7242 | 7242 |
| unsegmentedOpenLeads | 0 | 0 | 0 |
| hotLeads | 18 | 18 | 18 |
| warmLeads | 1118 | 1118 | 1118 |
| coldLeads | 6106 | 6106 | 6106 |

Resposta do endpoint (1º disparo): leadsScored=7242, hot=18, warm=1118, cold=6106, followUpTasksCreated=10

Resposta do endpoint (2º disparo, idempotência): leadsScored=7242, hot=18, warm=1118, cold=6106, followUpTasksCreated=3

## Critérios de sucesso da fase

1. **Score gravado pelo motor oficial C#, não pelo script Python de auditoria.** O único endpoint chamado nesta fase foi `POST /api/v1/pipeline/recompute-scores`, que internamente executa `LeadScoringService.RecomputeAllAsync`. `scripts/backfill_lead_scores.py` não contém nenhuma fórmula de score/segmento — é orquestração HTTP pura (login, POST, captura de snapshot via `GET /api/v1/sales-dashboard` e `GET /api/v1/leads`). `POST /api/v1/outreach/segment` (motor legado 0-6 pontos de `OutreachService.SegmentLeadsAsync`) nunca foi chamado em nenhum momento da fase.

2. **Leads Inactive (1.065) não repontuados.** `ICustomerRepository.GetAllLeadsAsync` (a query de origem de `RecomputeAllAsync`) filtra `Status < CustomerStatus.Customer`, excluindo estruturalmente `Status = Inactive(5)` e `Status = Churned(6)`. Essa garantia está travada por teste de regressão desde o plano 09-01 (`api-core/tests/Diax.Tests/Customers/CustomerRepositoryLeadFilterTests.cs`, suíte subiu de 891 para 894 testes, 100% verde). Evidência operacional: `openLeadsTotal` (soma dos 4 estágios abertos do funil) permaneceu em 7242 nos três snapshots — os contadores de segmento (Hot+Warm+Cold=18+1118+6106=7242) batem exatamente com `openLeadsTotal`, sem absorver os 1.065 Inactive em nenhum momento.

3. **Re-rodar é seguro.** Diff entre `backfill_after.json` e `backfill_after_rerun.json` nas 5 métricas escalares (`hotLeads`, `warmLeads`, `coldLeads`, `openLeadsTotal`, `unsegmentedOpenLeads`): **vazio** — `hotLeads=18, warmLeads=1118, coldLeads=6106, openLeadsTotal=7242, unsegmentedOpenLeads=0` idênticos nos dois. `leadsScored=7242` nas duas chamadas (o guard `if (lead.LeadScore != score || lead.Segment != segment)` de `RecomputeAllAsync` não gerou nenhuma divergência de contagem entre as duas execuções).

4. **Segmento coerente com o teto de fit 45 do LeadScoringService, não com o teto 25 do script ad-hoc.** A amostra Hot mostra `leadScore` até 90 (ex.: id `d29e37f4-c81a-4a14-85c2-d616bc19ba1a`, score 90), acima do teto 25 do script Python `outreach_priority_valid_leads.csv` — só alcançável com o bloco de engajamento/intenção (max 35) somado ao fit (max 45) do motor oficial C#. Nenhum `leadScore` da amostra ficou na escala 0-6 do motor legado.

## Amostra antes/depois

| Id (abreviado) | leadScore antes → depois | segment antes → depois |
|---|---|---|
| fefbc0f0 (Lojas Renner S) | 73 → 73 | Hot(2) → Hot(2) |
| 1dd8f994 (Specs Predator GM7) | 35 → 35 | Warm(1) → Warm(1) |
| 0849fa5c (Vixtur Agencia De Viagens) | 25 → 25 | Cold(0) → Cold(0) |

## Leitura do resultado

`unsegmentedOpenLeads` já era 0 no **baseline**, antes de qualquer disparo deste plano — não caiu durante a execução porque não havia o que cair. Isso é o cenário do Pitfall 2 da pesquisa (09-RESEARCH.md): o `LeadScoringWorker` diário (09:00 UTC) já havia alcançado o backlog inteiro antes deste plano rodar, provavelmente em uma janela recente sem redeploy interveniente. O disparo explícito via `POST /api/v1/pipeline/recompute-scores` não é redundante mesmo assim — ele prova deterministicamente (não por inferência do worker passivo) que o estado está correto, e a idempotência confirma que rodar de novo não desestabiliza nada.

A maioria do backlog aberto está em Cold (6106 de 7242 = 84,3%), com apenas 1118 em Warm e 18 em Hot. Isso é o comportamento **correto e esperado**, não um bug: os scripts de auditoria de 2026-09-17/19 (`fetch_and_classify_leads.py`, `lead_quality_filter.py`, `site_check.py`, `mx_check.py`) só escreveram `Status` de volta ao CRM (via `mark_junk_inactive.py`) — nunca `WebsiteKind`, `Quality` ou `EmailType`. Sem esses sinais da Phase 7 preenchidos, os bônus `WebsiteKind.OwnSite +10`, `Quality.High +5` e `EmailType.PersonalDirect +5` valem zero para praticamente todo o backlog legado, e o teto de fit efetivo cai de 45 para 25 — exatamente o teto do script ad-hoc que este backfill substitui. Um lead do backlog só sobe de Cold sem esses sinais através de engajamento real (abertura/clique de email) ou por já estar em Qualified/Negotiating (+15 de intenção), o que explica os 1136 leads em Warm/Hot observados. Backfillar `WebsiteKind`/`Quality`/`EmailType` do backlog legado é candidato a fase futura, explicitamente fora do escopo da BFIL-01.

## Efeito colateral aceito (D-09-01)

O 1º disparo criou 10 tasks de follow-up (`followUpTasksCreated=10`, batendo exatamente no teto `MaxFollowUpsPerRun`); o 2º disparo (prova de idempotência) criou mais 3 (`followUpTasksCreated=3`) — total de 13 tasks de follow-up geradas pela execução completa deste plano, para leads Hot do backlog que ainda não tinham uma task de follow-up pendente (não é um "score mudou" — os scores já estavam estáveis nos dois disparos; a criação de follow-up é uma varredura independente do guard de persistência de score). Isso é aceito por D-09-01 (registrada no plano 09-01): é o comportamento de produção já vigente do `LeadScoringWorker` diário, e nenhum bypass (`followUpOwnerUserId: null`) foi construído para suprimi-lo. O usuário deve revisar a amostra dessas tasks no checkpoint deste plano antes de decidir se o comportamento permanece como está ou se D-09-01 é revisitada.

## Revisão do usuário no checkpoint (09-19)

Usuário revisou os números desta evidência (confirmados corretos, idempotentes, `unsegmentedOpenLeads`
permaneceu 0) e a amostra das 13 tasks de follow-up. Aprovado, com uma correção pontual: 3 das 13 tasks
eram contaminação de scraping, não leads de negócio reais, e foram marcadas concluídas via
`mcp__diax-crm__complete_task` (ação de limpeza de task list no CRM, fora do escopo de arquivo/código
deste plano — nada para commitar por essa ação em si):

- `【官方评测】YOGA Pro 16 Aura 2026 AI元启版官方评测` (lenovoclub@lenovo.com) — página de review de
  produto chinês raspada como se fosse nome de empresa
- `Endereços E Contatos` (p.civ.serra@mpes.mp.br) — Ministério Público do Espírito Santo, órgão público,
  não é lead comercial
- `Informatica` (pr@informatica.com) — nome/email genérico, provável ruído

As outras 10 tasks de follow-up foram confirmadas como leads de negócio reais e legítimos (Lojas Renner,
Fatta Arquitetura, Cury Construtora, Habras Construtora, Lojas Dress To, entre outras) e permaneceram
como estão.

**Leitura para fases futuras:** esta contaminação (scraping internacional/institucional infiltrado no
backlog) é o mesmo padrão observado na auditoria de 2026-09-17/19 (`fetch_and_classify_leads.py` +
`lead_quality_filter.py`) — não é algo que a BFIL-01 poderia corrigir, porque é upstream do
`LeadScoringService`: o motor de score roda corretamente sobre o dado que recebe, mas o dado de entrada
já veio contaminado da extração/import. Um filtro melhor na origem (geo/idioma/tipo de organização) é
candidato de melhoria para a Phase 10 (consolidação de scripts) ou Phase 12 (escala de email), não para
esta fase.
