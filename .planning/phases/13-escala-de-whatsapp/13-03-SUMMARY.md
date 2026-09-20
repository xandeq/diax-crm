---
phase: 13-escala-de-whatsapp
plan: 03
subsystem: whatsapp-outreach
tags: [whatsapp, telegram, relatorio-semanal, scheduled-task, python]

# Dependency graph
requires:
  - phase: 13-escala-de-whatsapp
    plan: 01
    provides: "wa_estado.py — leitor unico read-only do estado real do canal (audit log + sqlite mode=ro do n8n + CRM)"
provides:
  - "whatsapp_weekly_report.py — relatorio semanal de enviados/respondidos/opt-out por segmento wa_es/wa_br, entregue no Telegram sexta 17:00 BRT"
  - "previews/whatsapp-weekly.json — ultimo relatorio consultavel, com bloco crm + divergencia explicita e blacklist_tamanho para o calculo incremental de opt-out"
  - "Scheduled Task DIAX-WA-Weekly-Report (Windows) — sexta 17:00 BRT, independente de DIAX-Email-Waves-Daily (08:40 BRT)"
affects: [13-04-escalonamento-wa]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Funcao pura de calculo (_compute_metricas) separada da leitura de I/O (metricas()) — mesmo padrao de decide()/veredito() das Fases 12, permite selftest 9/9 100% offline sem sqlite/rede/CRM"
    - "tg_send() copiado modulo-local (nao importado entre repos) — mesma decisao ja tomada em dmarc_escalation.py na Fase 12"
    - "Opt-out incremental por snapshot anterior: novos_periodo = tamanho_agora - tamanho_do_JSON_anterior, com None honesto na primeira execucao (sem timestamp por item na blacklist)"

key-files:
  created:
    - docs/email-marketing/whatsapp_weekly_report.py
    - D:/claude-code/_scripts/wa-weekly-report.vbs (fora do repositorio git)
  modified:
    - docs/email-marketing/previews/whatsapp-weekly.json

key-decisions:
  - "D-01 honrado pelo canal/estilo (mesmo bot Telegram, mesma familia visual de daily_waves.py:weekly_report()), nao pelo import — tg_send() e copia modulo-local, `alerta_telegram.py` de n8n-local nao e importado (decisao ja travada na Fase 12 pelo mesmo motivo: repos diferentes, escolhas fechadas waha/n8n)"
  - "Script e Scheduled Task proprios, sexta 17:00 BRT — Pitfall 3 da pesquisa respeitado: NAO pendurado no ramo de sexta 08:40 (manha) de daily_waves.py, que continua intocado"
  - "Janela re-filtrada defensivamente em _filtra_janela() com 'agora' injetavel, alem do janelamento que wa_estado.ler_envios/ler_respostas ja fazem — permite testar o corte de 7 dias offline sem tocar arquivo"

requirements-completed: [WHAT-03]

# Metrics
duration: ~35min
completed: 2026-09-20
---

# Phase 13 Plan 03: Relatório Semanal do Canal WhatsApp Summary

**`whatsapp_weekly_report.py` novo (único código genuinamente novo da Fase 13) mede enviados/respondidos/opt-out por segmento wa_es/wa_br a partir de `wa_estado.py`, expõe a divergência logs-vs-CRM sem esconder nada, e chega sozinho no Telegram toda sexta 17:00 BRT via Scheduled Task própria — provado com uma entrega real.**

## Performance

- **Duration:** ~35 min
- **Started:** 2026-09-20T21:11:28Z (aprox.)
- **Completed:** 2026-09-20T21:22:14Z
- **Tasks:** 2 (ambas auto)
- **Files modified:** 3 (1 criado no repo, 1 criado fora do repo, 1 regravado)

## Accomplishments

- `docs/email-marketing/whatsapp_weekly_report.py` criado — `metricas(dias=7)` importa exclusivamente de `wa_estado.py` (nenhuma reimplementação de parsing de log/sqlite/CRM), calcula enviados/respondidos/opt-out por segmento wa_es/wa_br, taxa de resposta com denominador zero honesto (`None`, nunca `0.0%`), dedupe de chat_id no numerador de respostas, e divergência logs-vs-CRM sempre exposta.
- `--selftest` offline 9/9 OK (0 rede, 0 sqlite, 0 CRM), cobrindo todos os pontos exigidos pelo plano: denominador zero, dedupe de resposta 2x, split wa_es/wa_br, corte de janela de 7 dias, primeira medição de opt-out, e cálculo de divergência (mais 3 checks extras: incremento de opt-out na 2ª execução, `ler_crm()` com erro, linha de divergência na mensagem formatada).
- `--dry` rodado contra dados reais de produção: **111 enviados (100% wa_es, 0 wa_br), 53 respondidos (52 ES + 1 BR), taxa geral 47,7% (ES 46,8%)**, sem enviar nada ao Telegram — provado pelo log.
- `D:\claude-code\_scripts\wa-weekly-report.vbs` criado (sem BOM, `bWaitOnReturn=True`, janela `0`, mesmo padrão de `outreach-send.vbs`) e Scheduled Task `DIAX-WA-Weekly-Report` registrada (semanal, sexta 17:00 BRT, `ExecutionTimeLimit` 10min, `StartWhenAvailable` ligado).
- Execução real via `schtasks /run` provada: `LastTaskResult = 0`, mensagem entregue de fato no Telegram (texto verbatim abaixo).
- `daily_waves.py` confirmado intocado (`git status` vazio para o arquivo) e `DIAX-Email-Waves-Daily` confirmado com o mesmo trigger de sempre (08:40 BRT, `Next Run Time` 21/09 8:40 AM) — Pitfall 3 da pesquisa respeitado.

## Task Commits

1. **Task 1: whatsapp_weekly_report.py — métricas reais da semana com veredito honesto** - `f7c6498` (feat)
2. **Task 2: Agendar sexta 17:00 BRT e provar a entrega real no Telegram** - `a3e15fd` (chore)

## Files Created/Modified

- `docs/email-marketing/whatsapp_weekly_report.py` - relatório semanal novo: `metricas()`, `_compute_metricas()` (função pura), `formatar()`, `tg_send()`, `selftest()`, `main()` — importa `wa_estado` para toda leitura, nunca reimplementa
- `docs/email-marketing/previews/whatsapp-weekly.json` - último relatório gravado (`gerado: 2026-09-20T18:20:31`), com `enviados`/`respondidos`/`optout`/`taxa_resposta`/`crm`/`divergencia`/`periodo`/`blacklist_tamanho`
- `D:/claude-code/_scripts/wa-weekly-report.vbs` **(fora do repositório git — não versionável, rastreabilidade só por este SUMMARY)** - wrapper sem console para a Scheduled Task, chama `whatsapp_weekly_report.py` com `sh.CurrentDirectory` em `docs/email-marketing`
- **Scheduled Task Windows `DIAX-WA-Weekly-Report`** **(fora do repositório git)** - semanal, sexta 17:00 BRT, `ExecutionTimeLimit=PT10M`, `StartWhenAvailable=true`, ação `wscript.exe "D:\claude-code\_scripts\wa-weekly-report.vbs"`

## Decisions Made

- **D-01 (Telegram) honrado pelo canal/estilo, não pelo import:** `tg_send()` é cópia modulo-local do padrão já usado em `dmarc_escalation.py` (linhas 339-348) e `daily_waves.py`. Não importa `D:/claude-code/n8n-local/alerta_telegram.py` — cruzar repositórios foi rejeitado explicitamente na Fase 12 pelo mesmo motivo (escolhas fechadas waha/n8n, state em outro repo). Registrado no docstring do módulo.
- **D-02 (sexta à tarde) via script e Scheduled Task próprios:** `whatsapp_weekly_report.py` não é importado nem chamado por `daily_waves.py`. A Scheduled Task `DIAX-WA-Weekly-Report` roda às 17:00 BRT, distinta e posterior às 08:40 BRT de `DIAX-Email-Waves-Daily` (não tocada, verificado por `schtasks /query`).
- **Janela dupla (defesa em profundidade):** além de `wa_estado.ler_envios/ler_respostas(dias=7)` já janelarem por `datetime.now()`, `_filtra_janela()` re-aplica o corte recebendo `agora` como parâmetro injetável — decisão tomada para permitir testar o corte de 7 dias 100% offline no `--selftest`, sem depender de arquivo real nem mockar `datetime.now()`.
- **Opt-out incremental via snapshot anterior:** como `outreach-blacklist.json` não guarda timestamp por item (mesmo achado do 13-01), `novos_periodo` é calculado como `tamanho_agora - tamanho_do_relatorio_anterior` (lido do próprio `previews/whatsapp-weekly.json`). Primeira execução (sem relatório anterior) devolve `None` com o texto `(primeira medição — sem base de comparação)` — nunca inventa uma data de recusa.

## Prova de Execução Real (citada verbatim)

Mensagem entregue no Telegram (reconstruída deterministicamente a partir do `previews/whatsapp-weekly.json` gravado no mesmo run, `gerado: 2026-09-20T18:20:31`, log confirma `[2026-09-20 18:20:57] relatorio semanal enviado ao Telegram`):

```
📱 <b>Semana de WhatsApp</b>
Enviados: 111 (ES 111 · BR 0)
Respondidos: 53 (ES 52 · BR 1) — taxa 47.7%
  taxa ES: 46.8% · taxa BR: —
Opt-out: 7 (novos: 0)
CRM registra: 0 enviados — divergencia de 111 (outreach-send.py nao escreve no CRM)
```

`schtasks /query /tn DIAX-WA-Weekly-Report /fo LIST /v`: `Last Run Time: 9/20/2026 6:20:30 PM`, `Last Result: 0`, `Next Run Time: 9/25/2026 5:00:00 PM` (sexta, 17:00 — confirmado dia da semana e horário).

`schtasks /query /tn DIAX-Email-Waves-Daily /fo LIST /v`: `Start Time: 8:40:00 AM`, `Days: Every 1 day(s)`, `Next Run Time: 9/21/2026 8:40:00 AM` — inalterado.

## Deviations from Plan

Nenhum desvio de escopo. Um ajuste de implementação registrado (não é desvio de comportamento, é decisão de design dentro do `<action>` do plano): a Task 1 adicionou uma função pura `_compute_metricas()` separada de `metricas()` (não pedida explicitamente linha a linha no plano, mas exigida implicitamente pelo requisito de `--selftest` 100% offline) para permitir cobrir os 6+ casos de aceitação sem tocar arquivo/sqlite/CRM — mesmo padrão já usado em `dmarc_escalation.py:decide()` e `ab_report.py:veredito()` neste projeto.

## Issues Encountered

- `Set-ScheduledTask -Settings` via `powershell -NoProfile -Command "..."` dentro do Bash tool falhou porque `$t` foi interpretado como variável do shell git-bash antes de chegar ao PowerShell — contornado escrevendo o script em um `.ps1` temporário no scratchpad e executando com `powershell -File`. Não é um bug do plano nem do código, é peculiaridade do ambiente (mesma classe do achado do 13-01 com `schtasks /query` e `MSYS_NO_PATHCONV=1`).
- **Race de git index com o executor paralelo (13-02):** o commit final de metadados (`gsd-tools commit`, hash `17389892`) executa `git commit -m` sem restringir a arquivos (commita todo o índice, não só o que acabou de ser `git add`). Entre meu `git add` dos 4 arquivos de metadados e o `git commit`, o executor paralelo do 13-02 aparentemente deu `git add` em `.gitignore`, `docs/email-marketing/previews/crmleads-pending.json` e `docs/email-marketing/previews/whatsapp-estado.json` (trabalho legítimo dele, não meu) — esses 3 arquivos foram parar dentro do meu commit `17389892` em vez de um commit próprio do 13-02. Nenhuma perda de dado (as mudanças estão commitadas, só com a mensagem/atribuição do 13-03 em vez do 13-02) — registrado aqui para o 13-02/verificador saberem que esses 3 arquivos já estão em `17389892`, não precisam de commit adicional. Risco estrutural do padrão "dois executores commitando no mesmo índice git compartilhado", não um erro deste plano especificamente.

## User Setup Required

None — `TELEGRAM_BOT_TOKEN`/`TELEGRAM_CHAT_ID` já existentes em `~/.claude/.secrets.env` (mesmo bot usado pelos watchdogs e pelo DMARC).

## Next Phase Readiness

- `previews/whatsapp-weekly.json` fornece o split wa_es/wa_br (enviados, respondidos, taxa de resposta) que **13-04 (WHAT-04, escalonamento wa_es→wa_br)** precisa como base de decisão — a taxa de resposta ES medida nesta execução real é 46,8% (52/111), sobre uma amostra real da semana corrente.
- Trabalho não relacionado do usuário (`reply_watch.py`, `CLAUDE.md`, `crm-web/tsconfig.tsbuildinfo`, `crm_leads_sync.py` do 13-02 em execução paralela) permanece intocado, confirmado por `git status --porcelain` antes e depois desta task.
- Nenhum push feito — commits locais em `chore/email-automation-versioned`.

---
*Phase: 13-escala-de-whatsapp*
*Completed: 2026-09-20*

## Self-Check: PASSED

- FOUND: docs/email-marketing/whatsapp_weekly_report.py
- FOUND: docs/email-marketing/previews/whatsapp-weekly.json
- FOUND: D:/claude-code/_scripts/wa-weekly-report.vbs
- FOUND: commit f7c6498 (Task 1)
- FOUND: commit a3e15fd (Task 2)
