---
phase: 12-escala-de-email
plan: 04
subsystem: email-marketing
tags: [python, webmail, ab-testing, daily-waves, telegram]

# Dependency graph
requires:
  - phase: 12-escala-de-email
    plan: 02
    provides: warmup_target()/max_per_seg()/guard em lockstep em waves_core.py/daily_waves.py
  - phase: 12-escala-de-email
    plan: 03
    provides: ab_report.py (report()/format_tg()/parse_variant()) — grouping por sufixo sA/sB
provides:
  - "waves_core.email_ok()/fetch_leads() com mode='corp'|'webmail' explícito — mode='corp' é o default byte-a-byte idêntico ao anterior"
  - "daily_waves.build_webmail_wave() — caminho paralelo pra leads de webmail comercial, cap 15/dia, marca 'auto wm', fora da rotação de 20 nichos e fora do A/B por construção"
  - "ab_report ligado no resumo de sexta de daily_waves.main()"
  - "7º check offline (modo_webmail) no waves_smoke.py cobrindo toda a matriz de comportamento do modo"
affects: [email-marketing, lead-scoring-supply, phase-12-remaining-plans]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Parâmetro mode='corp'|'webmail' explícito em função compartilhada (email_ok/fetch_leads), com default preservando comportamento existente para todos os 20 chamadores atuais — em vez de duplicar a função ou remover o filtro FREE_WEBMAIL"
    - "Caminho paralelo (build_webmail_wave) com conjuntos se/sd/sc PRÓPRIOS, cursor próprio (nenhum), state key própria (webmail_sent) — não compartilha estado com build_waves()"
    - "Poda de state por janela de 30 dias (mesmo padrão que fup_sent deveria ter tido, mas não teve — fup_sent hoje tem 523 entradas sem poda, achado registrado como lição, não corrigido neste plano)"

key-files:
  created: []
  modified:
    - docs/email-marketing/waves_core.py
    - docs/email-marketing/waves_smoke.py
    - docs/email-marketing/daily_waves.py

key-decisions:
  - "Cap diário webmail = 15 (meio da faixa 10-20 sugerida pelo CONTEXT.md) — domínio webmail tem reputação pior e a base já está com bounce >10%, conservador é o certo (decisão documentada verbatim no 12-04-PLAN.md, aplicada sem alteração)"
  - "Marca 'AQ - webmail - Presenca Digital - {data} auto wm' — 'webmail' no lugar do nicho (casa com a regex de FUP existente) e sufixo 'auto wm' no lugar de 'auto sA/sB' (fica FORA do A/B por construção, parse_variant() devolve None)"
  - "fetch_multi() ganhou mode='corp' (default, retrocompatível) em vez de duplicar a função — os 2 chamadores existentes (build_waves, stock_runway) continuam sem passar mode e o comportamento deles é idêntico"

requirements-completed: [EMAL-04, EMAL-03]

# Metrics
duration: ~15min
completed: 2026-09-20
---

# Phase 12 Plan 04: Webmail Paralelo + A/B na Sexta Summary

**Caminho de disparo paralelo pra 719 leads webmail nunca-contactados (mode='webmail' exclusivo em email_ok/fetch_leads), cap 15/dia com marca 'auto wm' fora do A/B, mais o relatório de clique do 12-03 ligado no resumo de sexta.**

## Performance

- **Duration:** ~15 min
- **Completed:** 2026-09-20T16:19:43Z
- **Tasks:** 2/2 completas
- **Files modified:** 3 (`waves_core.py`, `waves_smoke.py`, `daily_waves.py`)

## Accomplishments

- **Contagem ao vivo dos leads webmail (2026-09-20), medida com os 24 domínios completos de
  `FREE_WEBMAIL`** (não a aproximação de 17/24 domínios da pesquisa):

  | Fonte | Valor |
  |---|---|
  | REQUIREMENTS.md (estático, data de coleta desconhecida) | 560 |
  | 12-RESEARCH.md (aproximado, `LIKE` sobre 17/24 domínios) | ~712 |
  | **Medido agora (24/24 domínios, query parametrizada a partir de `waves_core.FREE_WEBMAIL`)** | **719** |

  Breakdown por domínio (top 5 de 18 domínios com pelo menos 1 lead): `gmail.com` 531 ·
  `hotmail.com` 104 · `yahoo.com.br` 20 · `outlook.com` 15 · `uol.com.br` 10 (lista completa dos
  18 domínios na seção "Contagem ao vivo" abaixo). O valor que vale é o medido agora (719); tanto
  o 560 quanto o 712 estavam desatualizados/aproximados.

- **`waves_core.email_ok()`/`fetch_leads()` ganharam `mode='corp'|'webmail'` explícito**
  (assinatura nova: `email_ok(email, seen_emails, seen_domains, mode='corp')`). `mode='corp'` é
  o **default** e é comportamento byte-a-byte idêntico ao anterior — nenhum dos 20 nichos passa
  `mode`, então nada muda pra eles (confirmado: `git diff` do bloco `FREE_WEBMAIL = {...}` não
  mostra nenhuma linha alterada). `mode='webmail'` inverte o portão (só aceita domínios em
  `FREE_WEBMAIL`) e desliga a checagem de `dup-dominio` (senão o 2º gmail do dia morreria como
  duplicado). `mode` inválido levanta `ValueError` explícito — sem fallback silencioso.

- **`waves_smoke.py` ganhou o 7º check (`modo_webmail`)**, cobrindo literalmente todas as
  assertivas do bloco `<behavior>` do plano (8 assertivas de `email_ok` + `len(FREE_WEBMAIL)==24`
  + `ValueError` em modo inválido); docstring atualizada de "6 checks" pra "7 checks".

- **`daily_waves.build_webmail_wave(H, st)`** — caminho paralelo, chamado em `main()` logo depois
  de `build_waves()`. Cap 15/dia (`WEBMAIL_CAP`) rastreado em `state['webmail_sent']` (chave por
  data), com poda automática pra janela de 30 dias (`fup_sent` cresceu pra 523 entradas sem poda
  — não repetido aqui). Busca via `fetch_multi(..., mode='webmail')` com fallback pra página
  geral do `/api/v1/leads` (mesmo `mode='webmail'`) quando a busca por termo não rende o
  suficiente — webmail não se agrupa por nicho. Higiene extra: descarta `is_role_mailbox` e
  `is_foreign_lead` antes de enfileirar (log da contagem descartada). Usa o **último** slot do
  dia (`compute_wave_slots(...)[-1]`) — webmail vai depois das corporativas, é experimento.
  Reaproveita os mesmos blocos de tratamento de erro/`WAVE_ERRORS` de `build_waves()`. Campanha
  nasce com `name = 'AQ - webmail - Presenca Digital - {data} auto wm'` — casa com a regex de FUP
  (`AQ - (\w+) - ([^-]+) -`) mas **não** casa com `ab_report.parse_variant()` (devolve `None`),
  provando que a população webmail fica fora do A/B por construção.

- **`fetch_multi()` ganhou `mode='corp'` (default, retrocompatível)** em vez de duplicar a lógica
  — os 2 chamadores pré-existentes (`build_waves`, `stock_runway`) continuam sem passar `mode` e
  seu comportamento é idêntico; `build_webmail_wave` é o único chamador que passa
  `mode='webmail'`.

- **`ab_report.format_tg(ab_report.report(30))` ligado no ramo de sexta de `main()`**, mesmo
  formato try/except/log dos vizinhos (`weekly_report`, `niche_refresh`), import **dentro** do
  try (não paga custo do módulo nos dias que não são sexta).

- **Resumo do Telegram** ganhou uma linha `📬 Webmail (teste): N/15 — tag auto wm` (só aparece
  quando `build_webmail_wave` de fato monta uma campanha, mesmo padrão condicional do resto do
  resumo).

## Task Commits

Each task was committed atomically:

1. **Task 1: Contagem ao vivo + modo explícito corp|webmail na busca compartilhada** -
   `3eaf4ca8` (feat)
2. **Task 2: build_webmail_wave() com cap 15 e marca própria + A/B ligado na sexta** -
   `41993de6` (feat)

**Plan metadata:** (this commit) — `docs(12-04): complete Webmail Paralelo + A/B na Sexta plan`

## Files Created/Modified

- `docs/email-marketing/waves_core.py` — `email_ok()`/`fetch_leads()` com `mode` explícito;
  `FREE_WEBMAIL`/`FOREIGN_TLD` intocados (verbatim)
- `docs/email-marketing/waves_smoke.py` — check `modo_webmail` (7º), docstring "7 checks"
- `docs/email-marketing/daily_waves.py` — `WEBMAIL_CAP`/`WEBMAIL_SEG`/`WEBMAIL_MARK`/`WEBMAIL_MIN`,
  `build_webmail_wave()`, `fetch_multi(mode=...)`, chamada em `main()`, linha de resumo no
  Telegram, `ab_report` no ramo de sexta

## Decisions Made

- Cap diário webmail = **15** (meio da faixa 10-20 do CONTEXT.md) — decisão já travada no plano,
  aplicada sem alteração; justificativa: domínio webmail tem reputação pior e a base já está com
  bounce rate acima de 10%.
- Marca `AQ - webmail - Presenca Digital - {data} auto wm` — casa com a regex de FUP existente
  (`AQ - (\w+) - ([^-]+) -` → `('webmail', 'Presenca Digital')`, confirmado ao vivo) e fica fora
  do A/B por construção (`ab_report.parse_variant()` só reconhece sufixo `auto s[AB]`).
- `fetch_multi()` ganhou `mode` opcional em vez de duplicar a função — mantém DRY sem tocar no
  comportamento dos 2 chamadores existentes.
- Sem site-check dedicado pra webmail em `personalize_queue` (passa `{}` como `site_res`) — fora
  de escopo do plano; os emails saem com a intro padrão (sem achado personalizado do site).

## Deviations from Plan

None — plano executado exatamente como escrito. Todas as acceptance criteria automatizadas das
duas tasks foram verificadas literalmente (ver seção "Verificação ao vivo" abaixo).

## Issues Encountered

None.

## Verificação ao vivo (medida nesta execução, 2026-09-20)

**Contagem ao vivo completa (24/24 domínios de `FREE_WEBMAIL`, customers com email preenchido,
`email_opt_out` falso, nunca contatados):**

```
TOTAL webmail nunca-contactado: 719
  gmail.com: 531        outlook.com: 15       aol.com: 4
  hotmail.com: 104       uol.com.br: 10        globo.com: 3
  yahoo.com.br: 20      yahoo.com: 9          bol.com.br: 2
                        terra.com.br: 6        hotmail.com.br: 2
                        outlook.com.br: 5      live.com: 2
                                                me.com: 2
                                                oi.com.br: 1
                                                ig.com.br: 1
                                                gmail.com.br: 1
                                                zipmail.com.br: 1
```

**Modo corp inalterado (Task 1, todas as assertivas do bloco `<behavior>` do plano, exit 0 de cada):**

```
email_ok('joao@gmail.com', set(), set())                         -> (False, 'webmail-nao-corp')
email_ok('joao@gmail.com', set(), set(), mode='webmail')          -> (True, 'ok')
email_ok('contato@empresa.com.br', set(), set(), mode='webmail')  -> (False, 'nao-webmail')
email_ok('m@gmail.com', set(), {'gmail.com'}, mode='webmail')     -> (True, 'ok')
email_ok('m@gmail.com', set(), {'gmail.com'})                     -> (False, 'dup-dominio')
email_ok('a@b.com', set(), set(), mode='xpto')                    -> ValueError('mode invalido: ...')
len(FREE_WEBMAIL)                                                 -> 24
git diff do bloco FREE_WEBMAIL = {...}                            -> 0 linhas alteradas
```

**`waves_smoke.py` — 7/7 OK** (rodado após cada task):

```
OK rotation_coberta
OK copy_bank_integro
OK template_v2_identico
OK healthy_mutavel_in_place
OK imports_locais_resolvem
OK warmup_curva
OK modo_webmail
== 7/7 OK ==
```

**`python daily_waves.py --dry --force` (log `logs/daily-waves-2026-09-20.log`), sem traceback,
chega em `===== FIM =====`:**

```
[..] WARMUP dia 1/8 — alvo 87/dia · 4 provider(s) · MAX_PER_SEG=8
[..] DRY: alvo do dia = 87 (warmup dia 1)
[..] DRY: plano = [('marketing', 4), ('consultoria', 4)]
[..] WEBMAIL: 3 descartado(s) (role-mailbox/estrangeiro)
[..] DRY: webmail = 12 leads (cap 15, já hoje 0)
[..] ===== FIM =====
```

12 leads webmail sobreviveram à higiene extra (role-mailbox/estrangeiro descartou 3 de um total
apurado antes do filtro) — abaixo do cap de 15, mas acima do `WEBMAIL_MIN=5`, então uma campanha
teria sido montada numa execução real (não-dry).

**Prova de que webmail fica fora do A/B por construção:**

```python
ab_report.parse_variant('AQ - webmail - Presenca Digital - 2026-09-22 auto wm')  # -> None
re.match(r'AQ - (\w+) - ([^-]+) -', 'AQ - webmail - Presenca Digital - 2026-09-22 auto wm')
# -> ('webmail', 'Presenca Digital')  — regex de FUP não quebra
```

**`previews/daily-state.json` não ganhou a chave `webmail_sent` após o `--dry`** (confirmado:
`'webmail_sent' in d` → `False` — `save_state()` é sempre pulado em modo `DRY`, então nenhuma
escrita de estado ocorre, independente do que fica em memória durante a execução).

**Diff de `daily_waves.py` não toca `COPY_BANK`/`SHORT_SUBJ`/`SUBJ_B`/`ROTATION`** — confirmado
por grep no diff (0 ocorrências nas definições desses blocos).

## User Setup Required

None — sem configuração de serviço externo necessária. Nenhuma campanha real foi criada
(apenas `--dry`); a primeira execução não-dry via Task Scheduler é quem efetivamente dispara a
wave de webmail e o A/B de sexta.

## Next Phase Readiness

- Leads webmail (719 nunca-contactados) agora têm um caminho de disparo próprio, capado e
  identificável separadamente no relatório — pronto pra 12-05/12-06 (checkpoint de DMARC e
  verificação humana final da fase).
- O A/B de subject (12-03) agora aparece de fato no resumo de sexta, não só existe como módulo
  isolado — fecha o "reportado" de EMAL-03 (o "medido" já estava fechado no 12-03).
- Concern para o roadmap: sem site-check dedicado pra webmail (achado real do site na intro), os
  emails de webmail saem com a intro padrão — se a taxa de resposta da população webmail vier
  baixa, personalização por achado real é o próximo ajuste candidato (fora de escopo deste plano).

---
*Phase: 12-escala-de-email*
*Completed: 2026-09-20*

## Self-Check: PASSED

All created/modified files confirmed present on disk; both task commits (`3eaf4ca8`, `41993de6`)
confirmed in `git log`.
