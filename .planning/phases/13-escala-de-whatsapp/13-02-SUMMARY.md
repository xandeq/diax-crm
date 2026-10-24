---
phase: 13-escala-de-whatsapp
plan: 02
subsystem: whatsapp-outreach
tags: [whatsapp, n8n, sqlite, python, scheduled-task, crm-leads, deflection]

# Dependency graph
requires:
  - phase: 13-escala-de-whatsapp (13-01)
    provides: "wa_estado.py — leitor unico read-only do canal WhatsApp (ler_sent/ler_blacklist/ler_n8n), reusado sem reimplementar"
provides:
  - "crm_leads_sync.py — sync incremental, verificado e offline-safe do conjunto de deflexao (staticData.global.crmLeads) do bot 8nKTlX4Y7Hy1EUEn"
  - "Scheduled Task DIAX-WA-CrmLeads-Sync (hourly 09h-19h) mantendo o conjunto de deflexao sempre atualizado, sem depender de re-execucao manual de restaurar-tudo.py"
  - "Fila durave previews/crmleads-pending.json — nunca perde delta quando o n8n esta fora do ar"
affects: [13-04-escalonamento-wa]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Nucleo de decisao com I/O injetada (_executar(argv, fn_fontes, fn_atuais, fn_porta_aberta, fn_empurrar, fn_verificar, fn_pend_ler, fn_pend_gravar, fn_log)) para permitir --selftest 100% offline sem tocar rede/sqlite/CRM — mesmo espirito do decide() puro de dmarc_escalation.py (Fase 12), adaptado para um pipeline com efeitos colaterais em vez de uma decisao unica"
    - "Verificacao por releitura pos-escrita (grava_verificado()) aplicada a um sistema externo (sqlite do n8n) em vez de um arquivo proprio — a mesma licao do incidente de 12/09 (outreach-send.py) estendida para confirmar que o webhook do n8n realmente commitou o staticData"

key-files:
  created:
    - docs/email-marketing/crm_leads_sync.py
    - docs/email-marketing/previews/crmleads-pending.json
  modified:
    - docs/email-marketing/previews/whatsapp-estado.json
    - .gitignore

key-decisions:
  - "--reset manda a fonte INTEIRA (nao o delta) porque reset:true no webhook substitui o conjunto completo — usar delta ali corromperia o proposito do reset"
  - "docs/email-marketing/logs/ adicionado ao .gitignore para formalizar o padrao ja em vigor (daily-waves-*.log/dmarc-escalation.log/reply-watch.log nunca foram versionados) em vez de deixar crm-leads-sync.log gerado-mas-nao-rastreado"

patterns-established:
  - "Pipeline sync incremental com fila pendente durave: le fonte + atual, calcula delta, funde pendentes anteriores, tenta empurrar, verifica por releitura, o que nao confirmar volta para a fila — reutilizavel por qualquer sync futuro contra um sistema externo que pode estar fora do ar"

requirements-completed: [WHAT-02]

# Metrics
duration: ~20min
completed: 2026-09-20
---

# Phase 13 Plan 02: Sync Incremental do Conjunto de Deflexao do Bot WhatsApp Summary

**`crm_leads_sync.py` reescrito de um populador manual de 22 linhas para um sync incremental verificado (delta + fila pendente + releitura pos-push), agendado de hora em hora via `DIAX-WA-CrmLeads-Sync`, que capturou e colocou em fila 18 leads reais que `outreach-send.py` ja havia contatado mas que nunca haviam entrado no conjunto de deflexao do bot — o gap exato que WHAT-02 exige fechar.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-09-20T21:11:28Z (retomada da fase, leitura de contexto)
- **Completed:** 2026-09-20T21:27:00Z (commit final do Task 2)
- **Tasks:** 2 (ambas `type="auto"`, sem checkpoint)
- **Files modified:** 4 (`crm_leads_sync.py` criado; `previews/crmleads-pending.json` criado; `previews/whatsapp-estado.json` e `.gitignore` modificados) + 2 arquivos nao-versionaveis (`crm-leads-sync.vbs`, Scheduled Task)

## Accomplishments

- `docs/email-marketing/crm_leads_sync.py` reescrito do zero (407 linhas) com `fontes()`/`atuais()`/`delta()`/`empurrar()`/`verificar()`/`main()` exportados conforme o contrato do plano; nucleo de decisao (`_executar`) com I/O 100% injetada, permitindo `--selftest` cobrir 16 casos (normalizacao, delta vazio, fusao de pendentes, n8n fora do ar, verificacao com faltante, `--reset` isolado ao primeiro lote) sem nenhuma chamada real de rede/sqlite/CRM.
- Fila pendente durave `previews/crmleads-pending.json` implementada: nunca perde o delta quando o n8n esta fora do ar, nunca finge sucesso (sai codigo 0 com mensagem explicita `N8N FORA DO AR: N pendentes`).
- Verificacao por releitura direta do `staticData.global.crmLeads` do sqlite do n8n (SOMENTE-LEITURA) apos cada push — se algum id nao confirmar, sai com codigo 3 e o id volta para a fila.
- `D:\claude-code\_scripts\crm-leads-sync.vbs` criado (padrao `outreach-send.vbs`: sem BOM, `bWaitOnReturn=True`, janela 0) e Scheduled Task `DIAX-WA-CrmLeads-Sync` registrada (diaria 09:00, repeticao a cada 1h por 10h, `ExecutionTimeLimit` 10min, acao `wscript.exe`), executada de verdade via `schtasks /run`: `LastTaskResult=0`.
- **Prova real do gap fechado (nao hipotetica):** dos 18 ids no delta desta execucao, **todos os 18** sao chat_ids presentes em `outreach-sent.json` (leads que `outreach-send.py` genuinamente contatou), e **nenhum** estava em `staticData.global.crmLeads` antes desta task. Exemplo citavel: `<email-redacted>@redacted.invalid` (AONI IMOVEIS) recebeu mensagem real em `2026-09-15 09:00:11` e continuava fora do conjunto de deflexao ate este sync rodar — se esse lead tivesse respondido nesse intervalo de 5 dias, o bot teria respondido com IA em vez de desviar para humano, violando exatamente o que WHAT-02 exige.
- Como o n8n esta confirmado fora do ar (porta 5678 fechada, medido nesta sessao e ja documentado em 13-01), os 18 ids foram gravados em `previews/crmleads-pending.json` em vez de POSTados — serao aplicados automaticamente na proxima execucao horaria da task assim que o n8n voltar, sem exigir nenhuma acao manual.
- `previews/whatsapp-estado.json` re-gravado ao final (`wa_estado.py --json`): `crm_leads` continua em `1057` (nao mudou, porque nada foi de fato empurrado com o n8n fora do ar) — confirma que o mecanismo e honesto, nao inflaciona o numero.

## Task Commits

1. **Task 1: Reescrever crm_leads_sync.py como sync incremental, verificado e offline-safe** - `a07b6cbb` (feat)
2. **Task 2: Agendar o sync e provar a deflexao ponta a ponta** - `17389892` (ver "Deviations from Plan" — commit compartilhado com o plano paralelo 13-03)

**Plan metadata:** este SUMMARY + `state advance-plan` (a seguir)

## Files Created/Modified

- `docs/email-marketing/crm_leads_sync.py` - sync incremental completo (fontes/atuais/delta/empurrar/verificar/main), CLI `--status`/`--dry`/`--reset`/`--selftest`, log em `logs/crm-leads-sync.log`
- `docs/email-marketing/previews/crmleads-pending.json` - fila pendente durave, 18 chat_ids aguardando o n8n voltar
- `docs/email-marketing/previews/whatsapp-estado.json` - snapshot regravado ao final da Task 2 (`crm_leads=1057`, `porta_5678_aberta=false`)
- `.gitignore` - `docs/email-marketing/logs/` adicionado (formaliza padrao ja existente de logs nao versionados)
- `D:\claude-code\_scripts\crm-leads-sync.vbs` (NAO versionavel, fora de qualquer repositorio git) - wrapper sem console para a Scheduled Task
- Scheduled Task Windows `DIAX-WA-CrmLeads-Sync` (NAO versionavel) - diaria 09:00, repeticao 1h por 10h, `ExecutionTimeLimit` 10min

## Decisions Made

- **`--reset` manda a fonte inteira, nao o delta:** porque `reset:true` no webhook do bot substitui o conjunto completo (`s.crmLeads = {}` antes de aplicar) — calcular delta contra o `atual` faria sentido apenas no modo incremental; no modo reset, o objetivo e replantar o conjunto inteiro a partir da fonte corrente.
- **`docs/email-marketing/logs/` entra no `.gitignore`:** confirmado por `git ls-files` que nenhum log pre-existente no diretorio (`daily-waves-*.log`, `dmarc-escalation.log`, `reply-watch.log`, `whatsapp-weekly.log`) jamais foi versionado — formalizei o padrao em vez de deixar `crm-leads-sync.log` gerado-mas-nao-rastreado (regra do `task_commit_protocol`).
- **Prova de deflexao usou os 18 ids do delta real, nao um unico `chat_id` anedotico:** o plano sugeria citar `envios[-1]['chat_id']` (o envio mais recente), mas esse chat_id especifico ja estava em `atuais()` desde a carga original de `restaurar-tudo.py` (12/09) porque veio do mesmo arquivo-fonte. Os 18 ids do delta sao uma evidencia mais forte e mais honesta do gap real: leads contatados ha ate 5 dias (`2026-09-15`) que nunca entraram no conjunto de deflexao — exatamente o cenario que WHAT-02 existe para fechar.

## Deviations from Plan

### Auto-fixed Issues

Nenhum desvio de Rule 1-3 durante a implementacao — plano seguido como escrito no que diz respeito ao codigo e ao comportamento do sync.

### Execucao paralela — commit da Task 2 absorvido pelo commit final do plano 13-03

- **Encontrado durante:** Task 2, entre `git add` e `git commit` desta task.
- **O que aconteceu:** este executor roda em paralelo com o executor do plano 13-03, no MESMO working directory / index git (nao worktrees separadas). Entre meu `git add docs/email-marketing/previews/crmleads-pending.json docs/email-marketing/previews/whatsapp-estado.json .gitignore` e meu `git commit`, o executor 13-03 rodou o proprio `git commit` de fechamento de plano (via `gsd-tools.cjs commit`), que commita TODO o index no momento (nao apenas os arquivos citados na mensagem) — absorvendo meus 3 arquivos staged no commit `17389892` ("docs(13-03): complete relatorio semanal de whatsapp plan"), junto com os arquivos legitimos do 13-03 (`STATE.md`, `ROADMAP.md`, `REQUIREMENTS.md`, `13-03-SUMMARY.md`).
- **Impacto:** nenhum no conteudo — `git show 17389892 -- .gitignore` confirma que meu bloco exato (`docs/email-marketing/logs/`) esta presente e intacto; `crmleads-pending.json`/`whatsapp-estado.json` tambem estao no mesmo commit com o conteudo correto. O unico efeito e de atribuicao: a Task 2 deste plano nao tem um hash de commit exclusivamente seu, ele esta co-misturado com o de 13-03.
- **Por que nao corrigido:** reescrever historico (rebase/reset) enquanto outro executor pode ainda estar ativo no mesmo repositorio e mais arriscado que documentar a co-mistura — indo contra o Git Safety Protocol (nunca reset/rebase destrutivo sem pedido explicito do usuario). O conteudo esta correto e rastreavel via `git show 17389892`.
- **Commit onde o conteudo apareceu:** `17389892` (mensagem de 13-03, contem os 3 arquivos deste plano 13-02 Task 2 tambem).

---

**Total deviations:** 1 (colisao de commit por execucao paralela compartilhando working directory — nao e um bug de codigo, e uma condicao de corrida de infraestrutura de execucao).
**Impact on plan:** Zero no conteudo entregue; apenas a rastreabilidade do commit da Task 2 ficou co-misturada com outro plano, documentada aqui com o hash exato para auditoria futura.

## Issues Encountered

- `docs/email-marketing/crm_leads_sync.py` estava listado no frontmatter do plano como "git-tracked (repo diax-crm)", mas `git ls-files` confirmou que o arquivo de 22 linhas anterior nunca havia sido de fato commitado (aparecia como `??` no `git status`) — provavelmente uma imprecisao residual do momento do planejamento. Sem impacto: o rewrite foi commitado normalmente pela primeira vez (`git add` de um arquivo novo funciona identico a um modificado).
- n8n confirmado fora do ar durante toda a execucao (porta 5678 fechada, mesma medicao de 13-01) — a "PROVA DE DEFLEXAO" seguiu o ramo B do plano (n8n fora do ar), documentado acima com os 18 ids reais em vez de apenas 1 exemplo hipotetico.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- `DIAX-WA-CrmLeads-Sync` roda sozinha de hora em hora (09h-19h); os 18 pendentes atuais entram automaticamente no conjunto de deflexao assim que o n8n local voltar ao ar, sem nenhuma acao manual.
- WHAT-02 fecha `Complete`: o mecanismo de sync continuo existe, esta agendado, e provou capturar um gap real (nao hipotetico) de leads contatados sem deflexao.
- 13-04 (escalonamento wa_es->wa_br) pode reusar `wa_estado.py`/`crm_leads_sync.py --status` para checar o estado do conjunto de deflexao antes de decidir escalar, sem reimplementar leitura do sqlite do n8n.
- Trabalho nao relacionado do usuario (`reply_watch.py`, `CLAUDE.md`, `crm-web/tsconfig.tsbuildinfo`) permanece intacto e nao commitado, confirmado por `git status --short` antes e depois desta task.
- Nenhum push feito — commits locais em `chore/email-automation-versioned`.

---
*Phase: 13-escala-de-whatsapp*
*Completed: 2026-09-20*

## Self-Check: PASSED

- FOUND: docs/email-marketing/crm_leads_sync.py
- FOUND: docs/email-marketing/previews/crmleads-pending.json
- FOUND: docs/email-marketing/previews/whatsapp-estado.json
- FOUND: D:/claude-code/_scripts/crm-leads-sync.vbs
- FOUND: .planning/phases/13-escala-de-whatsapp/13-02-SUMMARY.md
- FOUND: commit a07b6cbb (Task 1)
- FOUND: commit 17389892 (Task 2 content, co-mixed with 13-03's final commit — see Deviations)
- FOUND: Scheduled Task DIAX-WA-CrmLeads-Sync
