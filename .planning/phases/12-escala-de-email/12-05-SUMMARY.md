---
phase: 12-escala-de-email
plan: 05
subsystem: infra
tags: [dmarc, dns, whm, email-deliverability, python]

# Dependency graph
requires:
  - phase: 12-01
    provides: dmarc_escalation.py (decide()/set_pct()/read_zone_dmarc(), --apply gated, zero DNS writes)
provides:
  - "--drill mode em dmarc_escalation.py: prova em produção do par escrita<->reversão contra registro descartável _gsddrill, sem tocar o _dmarc real"
  - "Resolução do checkpoint humano da Task 1: decisão prova-e-segura registrada"
  - "docs/email-marketing/DMARC-escalonamento.md — runbook EMAL-01 (estado, regra, drill, saúde, cadência, limites, reversão manual)"
  - "previews/daily-state.json semeado com dmarc_pct=25/dmarc_changed_at/dmarc_history (hold), sem escrita de DNS"
affects: [12-06-plan, future-dmarc-escalation-sessions]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Drill contra registro DNS descartável (_gsddrill) para provar write/verify/revert/cleanup antes de confiar o mecanismo a um registro de produção real"
    - "Fallback B documentado: reescrever um registro real com seu próprio valor atual, quando addzonerecord/removezonerecord não estão disponíveis, para exercitar localizar->editar->verificar com resultado semanticamente nulo"

key-files:
  created:
    - docs/email-marketing/DMARC-escalonamento.md
  modified:
    - docs/email-marketing/dmarc_escalation.py
    - docs/email-marketing/previews/daily-state.json
    - .planning/REQUIREMENTS.md
    - .planning/ROADMAP.md

key-decisions:
  - "Checkpoint da Task 1 resolvido com prova-e-segura: provar a reversão (feito, contra registro descartável) mas segurar o degrau real em pct=25, porque o bounce rate medido (5,08% em 7d, 10,58% em 30d) já excede o próprio limiar de reversão D-04 (>5%) travado pelo usuário — subir contrariaria a regra que ele mesmo definiu"
  - "EMAL-01 marcado Partial (não Complete) em REQUIREMENTS.md/ROADMAP.md: mecanismo de escrita/reversão provado em produção, mas o degrau real (pct=25->100) não avançou nesta fase, por escolha deliberada, não falha"
  - "Task 3 original (--set-pct 50 --apply contra o _dmarc real) NÃO executada por instrução explícita da resolução do checkpoint; adaptada para confirmar o estado inalterado + escrever o runbook + corrigir a rastreabilidade"

requirements-completed: []  # EMAL-01 permanece Partial — ver REQUIREMENTS.md Traceability, não marcado Complete

# Metrics
duration: 35min
completed: 2026-09-20
---

# Phase 12 Plan 05: Checkpoint DMARC + Drill de Reversão + Runbook Summary

**Drill de escrita/reversão DNS provado em produção contra registro descartável `_gsddrill`; degrau real do `_dmarc` segurado em `pct=25` por decisão deliberada do usuário (`prova-e-segura`), fiel ao próprio limiar de reversão D-04 que o bounce rate atual (5,08%/7d) já excede.**

## Performance

- **Duration:** ~35 min (retomada pós-checkpoint; a Task 1 — leitura, backup de zona, apresentação do gate — já havia sido executada em sessão anterior)
- **Tasks:** 2 de 3 executadas nesta retomada (Task 1 já concluída antes do checkpoint; Task 2 completa; Task 3 adaptada conforme resolução do checkpoint)
- **Files modified:** 5 (`dmarc_escalation.py`, `previews/daily-state.json`, `DMARC-escalonamento.md` criado, `REQUIREMENTS.md`, `ROADMAP.md`)

## Accomplishments

- **`--drill [--apply]` adicionado a `dmarc_escalation.py`**, provando em produção o caminho completo `addzonerecord → localizar por nome → editzonerecord(v2) → nslookup → localizar de novo → editzonerecord(v1, reversão) → nslookup → removezonerecord → dumpzone final` contra `_gsddrill.alexandrequeiroz.com.br.` (subdomínio descartável, sem serviço apontando para ele), com `try/finally` garantindo limpeza mesmo em falha parcial, e Fallback B documentado (não acionado — não foi necessário nesta execução).
- **Checkpoint da Task 1 resolvido**: decisão do usuário foi `prova-e-segura`. Executada exatamente como definido: drill provado, degrau real segurado.
- **`_dmarc` real nunca alterado** em nenhum momento desta execução — confirmado por `nslookup` no NS autoritativo antes e depois de todo o trabalho, valor idêntico byte a byte.
- **State semeado sem escrita de DNS**: `dmarc_pct=25`, `dmarc_changed_at=2026-09-20`, entrada em `dmarc_history` com `acao='hold'` e a saúde real — inicia o relógio de 7 dias para a próxima avaliação automática, sem exigir nenhuma ação de DNS.
- **`docs/email-marketing/DMARC-escalonamento.md` criado** (211 linhas, 8 seções: estado atual, curva/regra, comandos, prova do drill, saúde medida, cadência dos 3 degraus restantes, limites, reversão manual).
- **REQUIREMENTS.md e ROADMAP.md corrigidos** para refletir honestamente o estado: EMAL-01 passa de "Complete" (impreciso — implicava pct=100) para "Partial", com o motivo real documentado em prosa.

## Task Commits

1. **Task 2 (ferramenta): `--drill` mode em `dmarc_escalation.py`** - `6efdb1d4` (feat)
2. **Task 3 adaptada (parte A): seed de state hold, sem escrita de DNS** - `e62726d` (feat)
3. **Task 3 adaptada (parte B): runbook `DMARC-escalonamento.md`** - `834af08` (docs)

**Plan metadata:** commit final (docs: complete 12-05 plan) — feito ao final desta execução, junto com SUMMARY.md/STATE.md/ROADMAP.md/REQUIREMENTS.md.

_Nota: o drill em si (`--drill --apply`) foi executado duas vezes nesta sessão contra o registro descartável `_gsddrill` — a primeira para provar o mecanismo, a segunda (idêntica, sem efeito colateral persistente) apenas para capturar o log estruturado completo (incluindo os dois `nslookup` verbatim) para este SUMMARY e o runbook. Nenhuma das duas tocou o `_dmarc` real ou o state._

## Files Created/Modified

- `docs/email-marketing/dmarc_escalation.py` - Adicionado `DRILL_RECORD`, `dns_verify_name()`, `_drill_find()`, `_nslookup_retry()`, `_drill_fallback_b()`, `do_drill()`, e o dispatch `--drill` no `main()`. Nenhuma função pré-existente (`decide()`, `set_pct()`, `read_zone_dmarc()`) foi alterada.
- `docs/email-marketing/previews/daily-state.json` - Ganhou `dmarc_pct=25`, `dmarc_changed_at="2026-09-20"`, `dmarc_history` (1 entrada, `acao=hold`). Todas as 6 chaves anteriores (`cursor`, `fup_sent`, `crm_tasks`, `ab_flip`, `fup2_sent`, `fup3_sent`) preservadas intactas — confirmado por script (contagens: `fup_sent`=523, `crm_tasks`=21, `fup2_sent`=78, `fup3_sent`=28, `cursor`=4, `ab_flip`=false).
- `docs/email-marketing/DMARC-escalonamento.md` (novo) - Runbook EMAL-01 completo.
- `.planning/REQUIREMENTS.md` - EMAL-01: checkbox `[x]`→`[ ] (parcial — ver Traceability)`; linha da Traceability table reescrita com o motivo real, em vez de "Complete" genérico.
- `.planning/ROADMAP.md` - 12-05-PLAN.md marcado `[x]` com nota da decisão; linha `EMAL-01` do Coverage Map atualizada de "Pending" para "Partial (mecanismo provado, degrau segurado em pct=25 por decisão do usuário)"; Progress table da Phase 12 e "Plans: X/6" atualizados via `roadmap update-plan-progress`.

## Decisions Made

- **Checkpoint resolvido com `prova-e-segura`** (não `prova-e-sobe`, `so-drill` ou `abortar`): drill executado contra registro descartável; degrau real do `_dmarc` segurado em `pct=25`. Motivo verbatim registrado: o `--check` da própria ferramenta já dizia `hold` (bounce 5,08%/7d acima do limiar D-04 de 5%) — o usuário optou por respeitar esse sinal em vez de forçar avanço, que é exatamente o uso correto do mecanismo de segurança, não uma falha dele.
- **Task 3 original (`--set-pct 50 --apply` contra o `_dmarc` real) explicitamente NÃO executada**, por instrução direta da resolução do checkpoint recebida nesta retomada. Adaptada para: (a) reconfirmar `_dmarc` inalterado via `nslookup`, (b) semear o state de hold sem tocar DNS, (c) escrever o runbook, (d) corrigir a rastreabilidade.
- **EMAL-01 marcado `Partial`, não `Complete`**, em ambos `REQUIREMENTS.md` e `ROADMAP.md` — o requirement literal ("DMARC sobe de pct=25 para pct=100") não foi atingido nesta fase, e marcar como "Complete" seria enganoso. A fase 12-06 (runbook consolidado + verificação humana final) é o lugar formal para o fechamento honesto de EMAL-01..04, mas a rastreabilidade já reflete a verdade atual a partir deste plano.

## Deviations from Plan

### Auto-fixed Issues

Nenhum desvio das Rules 1-3 (bug/missing-critical/blocking) foi necessário — o `--drill` funcionou na primeira tentativa, sem exigir o Fallback B nem qualquer correção de bug.

### Desvio de escopo instruído (não uma Rule 1-4 — resolução de checkpoint explícita)

**Task 3 executada de forma diferente do texto literal do plano**, por instrução explícita recebida
na retomada pós-checkpoint (não uma descoberta durante a execução, não uma decisão autônoma do
executor): em vez de `--set-pct 50 --apply`, a Task 3 foi adaptada para confirmar o estado inalterado,
semear o hold no state, escrever o runbook, e corrigir a rastreabilidade — consistente com a decisão
`prova-e-segura` do usuário no checkpoint da Task 1.

---

**Total deviations:** 0 auto-fixes de código; 1 adaptação de escopo instruída explicitamente na resolução do checkpoint (documentada acima, não uma Rule 1-4).
**Impact on plan:** Nenhum. A adaptação é exatamente o comportamento que o próprio plano previu para a opção `prova-e-segura` (ver `12-05-PLAN.md`, Task 3, bloco `<action>`, ramo `prova-e-segura`).

## Issues Encountered

Nenhum. O drill funcionou de ponta a ponta na primeira execução real (`addzonerecord`/`removezonerecord` — não provados anteriormente neste projeto — funcionaram sem erro). O `nslookup` confirmou `gsd-drill-v2` e `gsd-drill-v1` nas duas verificações, dentro da primeira tentativa (sem precisar dos 2 retries adicionais previstos no plano).

## User Setup Required

None - nenhuma configuração externa necessária. O token WHM já estava configurado em `~/.claude/.secrets.env` de fases anteriores.

## Next Phase Readiness

- **12-06 (Runbook consolidado + verificação humana final)** pode prosseguir: o mecanismo de escrita/reversão do EMAL-01 está provado em produção, o estado do `_dmarc` está correto e documentado, e a rastreabilidade já não está mais desatualizada.
- **Cadência semanal pendente**: a partir de 2026-09-27 (7 dias após `dmarc_changed_at` semeado aqui), rodar `python dmarc_escalation.py --check` deve ser o próximo passo manual (ou via piggyback futuro em `daily_waves.py`, não implementado nesta fase) para reavaliar se a saúde da base já permite o primeiro avanço real 25→50.
- **Blocker real para o avanço de EMAL-01 não é técnico**: é qualidade de lead na entrada elevando o bounce rate acima do limiar de D-04. Candidato a fase futura (fora do escopo de v1.4/Phase 12), já registrado como tal no runbook §5.

---
*Phase: 12-escala-de-email*
*Completed: 2026-09-20*

## Self-Check: PASSED

Todos os arquivos citados neste SUMMARY existem em disco (`docs/email-marketing/DMARC-escalonamento.md`,
`docs/email-marketing/dmarc_escalation.py`, `docs/email-marketing/previews/daily-state.json`,
`.planning/phases/12-escala-de-email/12-05-SUMMARY.md`) e todos os 3 commits de task (`6efdb1d4`,
`e62726d`, `834af08`) existem em `git log --oneline --all`.
