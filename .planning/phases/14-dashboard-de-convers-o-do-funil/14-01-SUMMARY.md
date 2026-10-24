---
phase: 14-dashboard-de-convers-o-do-funil
plan: 01
subsystem: observability
tags: [playwright, dotnet-test, xunit, funnel-metrics, verify-only, documentation]

# Dependency graph
requires:
  - phase: 11-qualified-objetivo-e-automatico
    provides: "Critério automático Contacted→Qualified (QUAL-01/02) que torna o número de Qualified do funil confiável, não manual"
provides:
  - "Runbook DASHBOARD-funil-runbook.md documentando a fórmula BuildFunnel, a natureza cumulativa de conversionToNext, o caso Negociando=0→100% e o gap deferido do filtro por período"
  - "Evidência ao vivo reconferida (dashboard-funil-evidencia.json) provando que a fórmula de produção bate com o código"
  - "Harness reproduzível dashboard-funil-shot.mjs (Playwright) + screenshot real da página /dashboard-comercial renderizada em produção"
  - "DASH-01 fechado Complete na rastreabilidade (REQUIREMENTS.md + ROADMAP.md), sem contradição entre os dois"
  - "Milestone v1.4 fechado 6/6 fases"
affects: [dashboard-comercial, sales-dashboard, milestone-v1.4-closeout]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Verify-only phase pattern (mesmo de Phases 11/12/13): zero código de aplicação alterado, só evidência + runbook"
    - "Playwright screenshot harness modelado em crm-e2e.mjs (stealth context pt-BR/America/Sao_Paulo)"

key-files:
  created:
    - docs/email-marketing/previews/dashboard-funil-evidencia.json
    - docs/email-marketing/qa-e2e/dashboard-funil-shot.mjs
    - docs/email-marketing/qa-e2e/dashboard-funil-shot-report.json
    - docs/email-marketing/previews/dashboard-funil-screenshot.png
    - docs/email-marketing/DASHBOARD-funil-runbook.md
  modified:
    - .planning/REQUIREMENTS.md
    - .planning/ROADMAP.md

key-decisions:
  - "DASH-01 fecha Complete (não Partial) porque veredito==BATE E shot-report.ok==true, ambos satisfeitos na primeira e segunda execução do harness"
  - "Gap do filtro por período (critério #3 do ROADMAP) documentado como deferido, não escondido — DASH-01 fecha Complete com essa ressalva explícita, não Partial, porque o gap é de escopo (D-01 verify-only), não de qualidade da implementação existente"
  - "Fase de fechamento: milestone v1.4 concluído 6/6 fases (9-14), com 11/14 requisitos Complete e 3 Partial (EMAL-01/02/04) por decisão deliberada/calendário já registrada nas Fases 12/13"

patterns-established:
  - "Runbook de fechamento de milestone: última fase de um milestone deve incluir seção dedicada resumindo o estado dos 14 requisitos, não só do próprio requisito da fase"

requirements-completed: [DASH-01]

# Metrics
duration: 20min
completed: 2026-09-20
---

# Phase 14 Plan 01: Dashboard de Conversão do Funil — Runbook e Fechamento Summary

**Reconferência ao vivo da fórmula `BuildFunnel` (veredito BATE) + prova visual real via Playwright do `/dashboard-comercial` em produção, publicadas em runbook que fecha DASH-01 e o milestone v1.4 inteiro (6/6 fases)**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-09-20T22:50:00Z (aprox.)
- **Completed:** 2026-09-20T23:11:53Z
- **Tasks:** 3
- **Files modified:** 7 (5 criados, 2 atualizados)

## Accomplishments

- Login real em produção + `GET /api/v1/sales-dashboard` capturado ao vivo nesta execução
  (`2026-09-20T20:02:07-03:00`), com re-derivação manual das 5 etapas do funil (`ahead`/`basis`/
  `esperado`) comparada ao `conversionToNext` retornado pela API — **veredito `BATE`**, zero
  discrepância dentro de `1e-4`.
- Prova visual real: screenshot Playwright (114,6 KB) da página `/dashboard-comercial/` de produção,
  logada com credencial admin real, mostrando o bloco "Funil de vendas" com as 5 etapas e os
  percentuais — 4/4 checks de DOM `true`, `ok: true` no report.
- Runbook `docs/email-marketing/DASHBOARD-funil-runbook.md` (282 linhas) publicado, documentando o
  achado central da pesquisa (a natureza **cumulativa**, não adjacente, de `conversionToNext`), o
  caso `Negociando=0 → 100%` com os números reais desta execução, e o gap deferido do filtro por
  período (critério #3 do ROADMAP) — explicitamente não escondido.
- `dotnet test --filter "FullyQualifiedName~SalesDashboardTests" -c Release`: 5/5 passou, zero
  arquivo de aplicação (`SalesDashboardService.cs`, `SalesDashboardController.cs`, `page.tsx`,
  `salesDashboard.ts`) tocado em nenhuma das 3 tasks (`git diff --stat` vazio confirmado).
- DASH-01 fechado `Complete` em `.planning/REQUIREMENTS.md` e `.planning/ROADMAP.md`, com o mesmo
  texto de status nos dois documentos — sem a contradição que o verificador havia encontrado na
  Phase 12.

## Task Commits

Each task was committed atomically:

1. **Task 1: Capturar AO VIVO os números do funil e re-derivar cada conversionToNext à mão** -
   `2666a784` (feat)
2. **Task 2: Prova visual — screenshot real de /dashboard-comercial renderizado em produção** -
   `3a8aa687` (feat)
3. **Task 3: Publicar o runbook DASH-01 e fechar a rastreabilidade + o milestone v1.4** -
   `44a4cf74` (docs)

_Nenhuma task usou TDD — fase verify-only, sem código de aplicação novo além do harness de evidência
(Playwright script, que não é código de produção)._

## Files Created/Modified

- `docs/email-marketing/previews/dashboard-funil-evidencia.json` - captura ao vivo da API + re-derivação etapa a etapa + veredito `BATE`
- `docs/email-marketing/qa-e2e/dashboard-funil-shot.mjs` - harness Playwright reproduzível (login real + screenshot + 4 checks de DOM + comparação contra a API)
- `docs/email-marketing/qa-e2e/dashboard-funil-shot-report.json` - report da execução do harness (`ok: true`, `bate_exato: true`)
- `docs/email-marketing/previews/dashboard-funil-screenshot.png` - prova visual real da página renderizada (114,6 KB)
- `docs/email-marketing/DASHBOARD-funil-runbook.md` - runbook DASH-01 completo (282 linhas, 10 seções)
- `.planning/REQUIREMENTS.md` - DASH-01 marcado `[x]` + linha de Traceability fechada `Complete`
- `.planning/ROADMAP.md` - Phase 14 `[x]`, Progress `1/1 Complete`, Coverage Map `DASH-01 Complete`, milestone v1.4 marcado `✅ Concluído 2026-09-20`

## Decisions Made

- **DASH-01 fecha `Complete`, não `Partial`**: a regra de honestidade do plano (Task 3, item E) exigia
  `Complete` apenas se `dashboard-funil-evidencia.json.veredito == "BATE"` **e**
  `dashboard-funil-shot-report.json.ok == true`. Ambas as condições foram satisfeitas — a primeira já
  na primeira execução, a segunda também na primeira execução do harness (os 4 checks de DOM já
  passaram de primeira; só a comparação informativa `comparacao_api.bate_exato` precisou de um ajuste
  de parsing na segunda execução do script, dentro do limite de 2 tentativas do plano, sem afetar o
  `ok` global).
- **Gap do filtro por período tratado como deferido, não como motivo de `Partial`**: o critério #3 do
  ROADMAP.md ("filtrar por período") não está implementado, mas isso é um gap de **escopo** (D-01
  trava a fase em verify-only, não em construir feature nova) — não uma falha do que já existe. O
  runbook e a rastreabilidade registram o gap explicitamente em seção separada, sem misturá-lo com o
  fechamento de DASH-01.
- **Milestone v1.4 encerrado nesta fase**: com Phase 14 sendo a última das 6 fases do v1.4, o
  ROADMAP.md e este SUMMARY registram o fechamento do milestone inteiro (ver seção dedicada abaixo).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Bug de parsing no script de comparação da Task 2 (não em código de aplicação)**
- **Found during:** Task 2, primeira execução de `dashboard-funil-shot.mjs`
- **Issue:** A comparação `comparacao_api` procurava os rótulos das etapas (`Leads`, `Clientes`) na
  primeira ocorrência do texto raspado da página, mas esses mesmos rótulos também aparecem no menu de
  navegação lateral (`Clientes`/`Leads` como itens de menu), **antes** da seção "Funil de vendas" no
  DOM. Isso causava `na_tela: null` para as etapas Lead e Customer, mesmo com os números corretos
  visíveis na tela (confirmado no screenshot).
- **Fix:** Escopar a busca de rótulos ao trecho do texto entre "Funil de vendas" e "Receita de
  propostas" antes de procurar cada etapa, evitando casar com os itens de menu.
- **Files modified:** `docs/email-marketing/qa-e2e/dashboard-funil-shot.mjs`
- **Verification:** Segunda execução do harness (2ª de no máximo 2 tentativas permitidas pelo plano)
  — `bate_exato: true`, todos os 5 deltas em `0`.
- **Committed in:** `3a8aa687` (commit único da Task 2, já com a correção aplicada — o bug foi
  corrigido antes do commit, não em commit separado)

---

**Total deviations:** 1 auto-fixed (Rule 1 — bug de script de evidência, não de aplicação)
**Impact on plan:** Correção necessária para que a comparação informativa `comparacao_api` refletisse
a realidade (os números já batiam desde a primeira execução; o bug era só no script de leitura do
DOM). Zero código de aplicação tocado. Nenhum scope creep — dentro do limite de 2 execuções do
harness definido pelo plano.

## Issues Encountered

Nenhum bloqueio. As duas execuções do harness Playwright (Task 2) ficaram dentro do limite de 2
tentativas do plano; a segunda foi necessária só para corrigir a comparação informativa
(`comparacao_api`), não os 4 checks obrigatórios de DOM, que já haviam passado na primeira execução.

## User Setup Required

None - nenhuma configuração de serviço externo necessária. Credenciais lidas de
`C:/Users/acq20/.claude/.secrets.env` (já existentes, `DIAX_ADMIN_EMAIL`/`DIAX_ADMIN_PASSWORD`).

## Next Phase Readiness

Não há próxima fase no v1.4 — este plano fecha o milestone inteiro. Ver seção "Fechamento do
milestone v1.4" abaixo para o estado consolidado e possíveis próximos passos (v1.5 ou retomada do
v1.2 pausado).

## Fechamento do milestone v1.4

Com DASH-01 fechado `Complete` nesta execução, **o milestone v1.4 "Funil de Outreach — Cadência e
Qualidade" está concluído: 6/6 fases entregues**:

| Fase | Requisitos | Status |
|---|---|---|
| 9. Backfill de Score do Backlog | BFIL-01 | Complete |
| 10. Motor de Cadência Único + Pipeline Fora do Notebook | SCRP-01, OBSV-01 | Complete |
| 11. Qualified Objetivo e Automático | QUAL-01, QUAL-02 | Complete |
| 12. Escala de Email | EMAL-01, EMAL-02, EMAL-03, EMAL-04 | EMAL-03 Complete; EMAL-01/02/04 **Partial** (decisão deliberada de segurança/calendário) |
| 13. Escala de WhatsApp | WHAT-01, WHAT-02, WHAT-03, WHAT-04 | Complete (WHAT-01 por equivalência funcional) |
| 14. Dashboard de Conversão do Funil | DASH-01 | Complete |

**Estado honesto dos 14 requisitos do v1.4: 11 `Complete`, 3 `Partial`.** Os 3 `Partial`
(EMAL-01/02/04) não são falhas de implementação — são mecanismos entregues e provados em produção
(drill de reversão de DMARC, curva de warmup, caminho webmail) cujo **degrau operacional final** foi
deliberadamente segurado por decisão do usuário: EMAL-01 porque o bounce rate medido já excede o
próprio limiar de reversão travado (D-04); EMAL-02/04 porque o usuário escolheu `aguardar` a execução
agendada de segunda-feira (2026-09-21 08:40 BRT) em vez de forçar um envio real fora de calendário num
domingo. Nenhum dos três representa código quebrado, não testado ou não provado — todos têm evidência
citável nos runbooks das Fases 12/13 (`DMARC-escalonamento.md`, `ESCALA-EMAIL-runbook.md`).

Zero `git push` executado em nenhuma das 3 tasks deste plano — todos os commits são locais, consistente
com a regra do projeto de que push exige autorização explícita do usuário (auto-deploy em `main`).

---
*Phase: 14-dashboard-de-convers-o-do-funil*
*Completed: 2026-09-20*

## Self-Check: PASSED

All 6 claimed files confirmed present on disk; all 3 task commit hashes (`2666a784`, `3a8aa687`,
`44a4cf74`) confirmed present in `git log --oneline --all`.
