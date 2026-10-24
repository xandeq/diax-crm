---
phase: 12-escala-de-email
plan: 03
subsystem: email-marketing
tags: [python, pyodbc, sql-server, ab-testing, email, reporting]

# Dependency graph
requires:
  - phase: 10-motor-unico-de-cadencia
    provides: "daily_waves.py build_waves() ja grava variante sA/sB no nome da campanha (SUBJ_B, ab_flip)"
provides:
  - "ab_report.py: modulo standalone/CLI que agrupa email_queue_items/email_events por sufixo sA/sB e calcula taxa de clique por variante"
  - "veredito(stats) puro: SEM-DADOS/INCONCLUSIVO/EMPATE/sA/sB com gate de amostra minima (MIN_CLICKS=30) e diferenca minima (MIN_DELTA=20%)"
  - "previews/ab-report.json: ultimo resultado medido (janela 30d), consultavel fora do Telegram"
  - "Achado documentado: 578/936 (61.8%) dos envios de 30d vem de campanhas 'Outreach Cold' fora do escopo sA/sB (outro fluxo de envio, nao daily_waves.py)"
affects: [12-04, 12-06]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Import preguicoso de daily_waves dentro de report() (nao no topo do modulo) para nao encarecer waves_smoke.py"
    - "sys.stdout.reconfigure(encoding='utf-8') no topo de scripts CLI que imprimem emoji/HTML, para sobreviver ao console cp1252 do Windows"

key-files:
  created:
    - docs/email-marketing/ab_report.py
    - docs/email-marketing/previews/ab-report.json
  modified: []

key-decisions:
  - "Vencedor do A/B decidido por taxa de CLIQUE (D-05), nunca por open rate; opens so aparecem como informativo no dict de saida e no format_tg, nunca em veredito()"
  - "MIN_CLICKS=30 e MIN_DELTA=20% sao discricionarios (CONTEXT.md deixou a criterio) — documentados no docstring do modulo com o raciocinio (amostra real de 12 cliques/30d tornaria qualquer limiar menor instavel semana a semana)"
  - "578/936 envios de 30d (61.8%) ficam fora do agrupamento sA/sB por vir de campanhas 'Outreach Cold - <data> <hora>' sem o sufixo 'auto s[AB]' — registrado como limitacao explicita do relatorio, filtro NAO relaxado para inflar a amostra"

patterns-established:
  - "Modulo de leitura/relatorio novo em docs/email-marketing/ nao deve importar modulos pesados (daily_waves) no topo — usar import dentro da funcao que precisa, protegido por try/except"

requirements-completed: [EMAL-03]

# Metrics
duration: 25min
completed: 2026-09-20
---

# Phase 12 Plan 03: A/B de Subject — Medição por Taxa de Clique Summary

**`ab_report.py` mede o A/B de subject que `daily_waves.py` já roda às cegas desde a Phase 10 — agrupa por sufixo `sA`/`sB`, decide por taxa de clique (nunca open rate), e declara `INCONCLUSIVO` honestamente com 12 cliques em 30 dias contra o gate de 30.**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-09-20T16:02:53Z (STATE.md, início da wave)
- **Completed:** 2026-09-20T16:12:00Z (aprox.)
- **Tasks:** 2/2
- **Files modified:** 2 (1 criado, 1 gravado como evidência)

## Accomplishments

- `ab_report.py` criado: `parse_variant`/`click_rate`/`open_rate`/`veredito` são funções puras, testadas offline via `--selftest` (9/9 assertivas do plano, exit 0)
- Consulta SQL única agrupa `email_queue_items`/`email_events` por `RIGHT(c.name,2)` filtrando `LIKE '% auto s[AB]'`, exatamente o padrão de `weekly_report()` que o plano mandou seguir
- `python ab_report.py --days 30 --write` rodou contra produção e gravou `previews/ab-report.json`: **veredito real = `INCONCLUSIVO`**, honesto e esperado dado o volume atual de cliques
- Achado de sanidade não previsto no baseline do plano: **61.8% dos envios de 30 dias (578/936) ficam fora do agrupamento sA/sB** — investigado, causa raiz identificada, documentado abaixo (não corrigido, fora de escopo)
- `waves_smoke.py` continua `6/6 OK`; `daily_waves.py` nunca foi tocado por este plano (12-02 edita o mesmo arquivo em paralelo nesta wave — diffs dele não entraram em nenhum commit deste plano)

## Task Commits

1. **Task 1: ab_report.py — agrupamento por variante, taxa de clique e veredito com gate de amostra** - `595aab6` (feat)
2. **Task 2: Rodar contra os dados reais, gravar previews/ab-report.json e registrar o veredito honesto** - `96e6284` (fix — inclui o fix de encoding descrito abaixo)

**Plan metadata:** (este commit, a seguir)

## Files Created/Modified

- `docs/email-marketing/ab_report.py` (191 linhas) — módulo standalone: `parse_variant`, `click_rate`, `open_rate`, `veredito`, `_query_stats`, `report`, `format_tg`, `_selftest`, CLI (`--days N`, `--json`, `--write`, `--selftest`)
- `docs/email-marketing/previews/ab-report.json` — último resultado medido (janela 30d, gerado 2026-09-20T16:07:52Z)

## Saídas `--json` verbatim (7d / 30d / 90d), rodadas ao vivo em 2026-09-20

### 7 dias
```json
{
  "janela_dias": 7,
  "gerado_em": "2026-09-20T16:07:39Z",
  "variantes": {
    "sA": {"sent": 12, "clicks": 0, "opens": 0, "bounces": 1, "dias": 1, "campanhas": 2},
    "sB": {"sent": 47, "clicks": 1, "opens": 1, "bounces": 2, "dias": 3, "campanhas": 5}
  },
  "veredito": "INCONCLUSIVO",
  "motivo": "1 cliques no total < 30 minimo para decidir",
  "subject_exemplos": {
    "logistica": ["controle de fretes", "fretes sem planilha"],
    "arquitetura": ["portfólio no google", "projetos no google"],
    "estudio": ["agendamento das aulas", "faltas nas aulas"]
  }
}
```

### 30 dias
```json
{
  "janela_dias": 30,
  "gerado_em": "2026-09-20T16:07:42Z",
  "variantes": {
    "sA": {"sent": 88, "clicks": 2, "opens": 2, "bounces": 7, "dias": 5, "campanhas": 17},
    "sB": {"sent": 270, "clicks": 10, "opens": 9, "bounces": 15, "dias": 8, "campanhas": 37}
  },
  "veredito": "INCONCLUSIVO",
  "motivo": "12 cliques no total < 30 minimo para decidir",
  "subject_exemplos": {
    "logistica": ["controle de fretes", "fretes sem planilha"],
    "arquitetura": ["portfólio no google", "projetos no google"],
    "estudio": ["agendamento das aulas", "faltas nas aulas"]
  }
}
```

### 90 dias
```json
{
  "janela_dias": 90,
  "gerado_em": "2026-09-20T16:07:44Z",
  "variantes": {
    "sA": {"sent": 364, "clicks": 4, "opens": 4, "bounces": 21, "dias": 14, "campanhas": 43},
    "sB": {"sent": 472, "clicks": 11, "opens": 12, "bounces": 33, "dias": 16, "campanhas": 64}
  },
  "veredito": "INCONCLUSIVO",
  "motivo": "15 cliques no total < 30 minimo para decidir",
  "subject_exemplos": {
    "logistica": ["controle de fretes", "fretes sem planilha"],
    "arquitetura": ["portfólio no google", "projetos no google"],
    "estudio": ["agendamento das aulas", "faltas nas aulas"]
  }
}
```

`previews/ab-report.json` no repositório é o snapshot de 30d (idêntico ao bloco acima, `gerado_em: 2026-09-20T16:07:52Z` — regravado após o fix de encoding, mesmos números).

## Conferência de sanidade contra o `COUNT(*)` independente (obrigatória, Task 2 passo 3)

Query independente rodada ao vivo:
```sql
SELECT COUNT(*) FROM email_queue_items
WHERE status=2 AND sent_at >= DATEADD(day,-30,SYSUTCDATETIME())
-- resultado: 936
```

Soma `sA.sent + sB.sent` do relatório 30d = 88 + 270 = **358**. Diferença = 936 − 358 = **578 envios (61.8%) fora do filtro `auto s[AB]`** — muito acima do limiar de 5% do plano.

**Causa raiz identificada** (query ao vivo, `GROUP BY c.name` dos 578 envios fora do filtro): 100% vêm de campanhas nomeadas `Outreach Cold - <data> <hora>` (ex.: `Outreach Cold - 2026-09-06 13:00`, 199 envios; `Outreach Cold - 2026-09-05 18:00`, 199 envios; `Outreach Cold - 2026-09-07 13:00`, 177 envios). Esse padrão de nome **não é** o de `daily_waves.py build_waves()` (`AQ - {seg} - {service} - {data} auto s{variant}`) — é o fluxo `OutreachController`/`Funnel Outreach` documentado no `CLAUDE.md` do projeto ("Auto-segmenta leads Hot/Warm/Cold, cooldown 7 dias, limite 200/dia"), um caminho de envio inteiramente separado que nunca recebeu o A/B de subject.

**Registrado como limitação explícita do relatório** (não silenciado, filtro não relaxado): `ab_report.py` mede corretamente 100% do que `daily_waves.py` envia, mas essa é hoje **apenas ~38% do volume total de email da conta**. Se o Outreach Cold também deve entrar no A/B de subject é uma decisão de escopo futuro, fora de EMAL-03 (que travou a infraestrutura de alternância em `daily_waves.py`/`SUBJ_B`/`ab_flip`).

## Achado adicional: desequilíbrio sA (88) vs sB (270) em 30d

Não é o cenário "`sent=0` numa variante" antecipado pelo plano, mas é um desequilíbrio real (~3.07x). Investigado com breakdown diário (`GROUP BY CONVERT(date, sent_at), variante`):

| Data | Variante | Enviados | Campanhas |
|---|---|---|---|
| 2026-08-24 | sB | 24 | 6 |
| 2026-08-25 | sA | 17 | 4 |
| 2026-08-25 | sB | 3 | 2 |
| 2026-08-28 | sB | 8 | 2 |
| 2026-08-31 | sA | 38 | 8 |
| 2026-09-02 | sB | 87 | 12 |
| 2026-09-04 | sB | 101 | 12 |
| 2026-09-07 | sA | 19 | 3 |
| 2026-09-08 | sA | 2 | 2 |
| 2026-09-16 | sB | 35 | 4 |
| 2026-09-18 | sA | 12 | 2 |
| 2026-09-18 | sB | 10 | 5 |
| 2026-09-19 | sB | 2 | 1 |

O flip **está** acontecendo (não travado num valor fixo — 5 dias-sA vs 8 dias-sB neste recorte), mas os dois dias de maior volume (`2026-09-02`: 87, `2026-09-04`: 101) caíram em `sB`, e há um gap sem envios de `2026-09-08` a `2026-09-16` (8 dias, consistente com trabalho concorrente de outras fases nesta sessão). `previews/daily-state.json` tem `"ab_flip": false` no momento desta medição, o que significa a próxima execução de `build_waves()` usa variante `A` (`variant = 'B' if ab_flip else 'A'`) — comportamento esperado, não travado. **Não é um "experimento quebrado"** no sentido do passo 5 da Task 2 (que previa `sent=0` como sinal de falha); é volume desigual por cadência irregular de execução. Registrado, não corrigido — fora de escopo mudar a lógica de alternância (EMAL-03 é medir).

## Veredito real e estimativa de decidibilidade

**Veredito 30d: `INCONCLUSIVO`, motivo "12 cliques no total < 30 minimo para decidir"** — este é o resultado correto e honesto de EMAL-03 dado o volume real de cliques, não uma falha do plano.

Estimativa: ritmo atual ≈ 12 cliques / 30 dias = 0,4 cliques/dia. Para atingir `MIN_CLICKS=30`, faltam 18 cliques → **~45 dias adicionais no ritmo atual (~75 dias corridos desde o início da janela medida)**. Esse prazo cai se o warmup de volume do 12-02 (87→300/dia) elevar o volume de envio proporcionalmente — mais envios/dia com a mesma taxa de clique ~2-4% produz mais cliques absolutos por dia.

## Decisions Made

- Métrica vencedora = taxa de clique, nunca open rate (D-05, travada pelo usuário) — `grep -n opens ab_report.py` confirma: linhas 9, 73, 77, 84, 113, 124, 144, 188 são comentário/docstring/`open_rate()`/dict de saída/query/`format_tg()` — nenhuma dentro da lógica de decisão de `veredito()`.
- `MIN_CLICKS=30`/`MIN_DELTA=0.20` ficaram como constantes discricionárias documentadas no próprio módulo (CONTEXT.md deixou a critério de implementação).
- Import de `daily_waves` (para `subject_exemplos`) feito dentro de `report()`, não no topo do módulo — evita encarecer `waves_smoke.py`, que importa todo `.py` do diretório.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] `UnicodeEncodeError` no console Windows (cp1252) ao imprimir `format_tg()`**
- **Found during:** Task 2, primeira execução de `python ab_report.py --days 30 --write` (sem `--json`)
- **Issue:** `format_tg()` usa emoji (`🅰️🅱️`) na primeira linha; o console padrão do Windows (`cp1252`) não sabe codificar esses caracteres, e o script quebrava com `UnicodeEncodeError` depois de já ter gravado `previews/ab-report.json` (o crash era só no `print` final, mas impedia capturar a evidência limpa pedida pela Task 2)
- **Fix:** `sys.stdout.reconfigure(encoding='utf-8', errors='replace')` / mesma coisa em `sys.stderr`, logo após os imports — mesma regra já documentada no projeto (memória global: "PYTHONIOENCODING=utf-8 nos scripts Windows"), aplicada via `reconfigure()` em vez de depender de variável de ambiente externa (mais robusto para uso interativo)
- **Files modified:** `docs/email-marketing/ab_report.py`
- **Verification:** `--selftest` (9/9) e `waves_smoke.py` (6/6) continuam passando; `--days N --json`/`--write` rodam sem traceback nas três janelas (7/30/90d)
- **Committed in:** `96e6284` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 blocking)
**Impact on plan:** Fix necessário para a própria Task 2 conseguir capturar evidência — sem ele, `--write` funcionava mas o comando `--json`/formato padrão travava. Nenhum scope creep: nada na lógica de medição/veredito foi alterado.

## Issues Encountered

- `daily_waves.py` apareceu como modificado (`git status --porcelain`) entre o commit da Task 1 e o início da Task 2 — confirmado via `git diff --stat` que é trabalho concorrente do plano `12-02` (execução paralela na mesma wave, conforme `<parallel_execution>`), não algo que este plano causou. Nenhum arquivo desse diff foi incluído em nenhum commit deste plano (`git add` sempre escopado a `ab_report.py`/`previews/ab-report.json`).

## User Setup Required

None - no external service configuration required. Leitura pura contra o banco de produção já configurado (`db.py`/`.secrets.env` existentes).

## Next Phase Readiness

- EMAL-03 está completo: medição existe, é rodável (`ab_report.py --days N`), grava resultado consultável (`previews/ab-report.json`), decide por clique, e declara `INCONCLUSIVO` honesto com estimativa de prazo.
- Achado a carregar para 12-04/12-06: 61.8% do volume de email (`Outreach Cold`) fica fora de qualquer medição de A/B de subject hoje — se isso deve mudar é decisão de escopo futuro, não bloqueia esta fase.
- Nenhum bloqueio para o restante da Phase 12.

---
*Phase: 12-escala-de-email*
*Completed: 2026-09-20*

## Self-Check: PASSED

- FOUND: docs/email-marketing/ab_report.py
- FOUND: docs/email-marketing/previews/ab-report.json
- FOUND: .planning/phases/12-escala-de-email/12-03-SUMMARY.md
- FOUND commit: 595aab6 (Task 1)
- FOUND commit: 96e6284 (Task 2)
