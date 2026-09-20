# Escala de Email — Runbook Consolidado (Phase 12, EMAL-01..04)

Gerado em: 2026-09-20 (Phase 12, plano 12-06)

Este documento consolida os quatro mecanismos que a Phase 12 acrescentou ao motor diário
(`daily_waves.py`, o motor único da Phase 10): escalonamento seguro de DMARC, warmup de volume,
medição de A/B de subject por clique, e wave paralela de leads webmail. Segue o mesmo formato dos
runbooks das Phases 10/11 (`OBSERVABILIDADE-watchdogs.md`, `QUALIFIED-avanco-automatico.md`): o que
é, onde roda, prova, limites, dependências. Nenhum mecanismo novo nasce aqui — este plano é
consolidação e verificação, não implementação (`git status --porcelain '*.py'` sai limpo de mudanças
deste plano; o único `.py` sujo no repositório, `reply_watch.py`, é trabalho não relacionado, em
andamento antes da Phase 11, preservado sem alteração).

---

## 1. O que a Phase 12 mudou

| # | Mecanismo | Arquivo principal | Comando de verificação | Estado real hoje (2026-09-20) |
|---|-----------|--------------------|--------------------------|-------------------------------|
| EMAL-01 | Escalonamento DMARC (advance/hold/revert, escrita gated) | `dmarc_escalation.py` | `python dmarc_escalation.py --status` / `--check` | Mecanismo provado (drill em produção contra `_gsddrill`); degrau real **segurado em `pct=25`** por decisão deliberada — ver §6 e `DMARC-escalonamento.md` |
| EMAL-02 | Warmup de volume 87→300/dia (+20%/dia) | `waves_core.py` (`WARMUP_TARGETS`) + `daily_waves.py build_waves()` | `python daily_waves.py --dry --force` (linha `WARMUP dia N/8`) | Curva e guard implementados e provados; volume real entregue limitado por estoque de lead por nicho, não pela curva — ver §3 |
| EMAL-03 | A/B de subject medido por taxa de clique | `ab_report.py` | `python ab_report.py --days 30 --json` | Medição implementada e rodável; veredito real = `INCONCLUSIVO` (amostra de cliques abaixo do mínimo) |
| EMAL-04 | Wave paralela de leads webmail (cap próprio) | `daily_waves.py build_webmail_wave()` | `python daily_waves.py --dry --force` (linha `DRY: webmail =`) | Caminho paralelo implementado e provado em `--dry`; execução real (envio de verdade) ainda pendente de aprovação humana — ver §7 e o checkpoint da Task 2 deste plano |

---

## 2. Warmup de volume (EMAL-02)

A curva é **literal**, não uma fórmula (`WARMUP_TARGETS = [87, 104, 125, 150, 180, 216, 260, 300]` em
`waves_core.py`) — decisão D-03 do CONTEXT.md, confirmada em código: `int(87*1.2**n)` daria
`87,104,125,150,180,215,259,311` e estouraria o teto de 300 no dia 8. `warmup_target(dia)` lê essa
lista, clampada em ambas as pontas (dia 1 nunca fica abaixo de 87, dia ≥8 nunca acima de 300).

`max_per_seg(target, n_seg, n_prov, min_seg)` deriva quantos leads por nicho tentar buscar a partir
do alvo do dia — é o que hoje se chama `MAX_PER_SEG` em `daily_waves.py`, recalculado a cada execução
(não mais um literal fixo de 20).

`WARMUP_KEEP_RATIO = 0.80` governa o avanço do degrau: `next_warmup_day()` só sobe um dia se o dia
anterior entregou ≥80% do alvo; caso contrário segura o degrau (`warmup_day` não muda) e o Telegram
mostra o alerta `📉 ABAIXO DO ALVO`. O avanço é avaliado no máximo uma vez por dia
(`warmup_last_date`), para a task não pular dois degraus se rodar duas vezes no mesmo dia.

**Por que o guard de fila deixou de ser `50`:** antes deste plano, `already_queued_today() >= 50` era
um literal fixo — com o alvo subindo até 300, esse guard truncaria a rampa em silêncio a partir do
dia 3 (Pitfall 4 da pesquisa da Phase 12: um guard pensado para "evitar duplicata" virava,
acidentalmente, um teto de volume). Agora o guard compara contra `target` (o alvo do dia), em
lockstep — nunca mais um número menor que o próprio alvo que se propõe a proteger.

**Onde ler o degrau atual:** `previews/daily-state.json`, chave `warmup_day` (inteiro 1-8) e
`warmup_last_date` (última data em que o degrau foi avaliado). Para forçar/corrigir manualmente: editar
`warmup_day` diretamente no JSON antes da próxima execução (não existe CLI dedicado para isso — é
estado simples, editável a mão em emergência, mesmo padrão dos outros contadores do arquivo como
`cursor`/`ab_flip`).

---

## 3. Realidade de estoque — o gargalo real de EMAL-02

**O teto hoje NÃO é o volume permitido pelo backend nem a curva de warmup.** O backend aceita até
900/dia (muito acima dos 300/dia-alvo). O gargalo real é **estoque de lead nunca-contactado por
nicho**, medido ao vivo nesta execução (2026-09-20, mesma query do 12-02, remedida):

| Métrica | Valor medido agora |
|---|---|
| Enviados (status=2) últimos 7 dias | 59 |
| Enviados últimos 14 dias | 260 (~18,6/dia) |
| Enviados últimos 30 dias | 936 (~31/dia) |
| Leads nunca-contactados, TOTAL (todos os domínios, `email_sent_count=0` e `last_email_sent_at IS NULL`) | 4.951 |
| … dos quais, domínio webmail (`FREE_WEBMAIL`, 24/24 domínios) | 719 |
| … dos quais, domínio corporativo (não-webmail) | 4.232 |
| Leads corporativos **efetivamente alcançáveis** pela busca por termo dos 20 nichos da rotação, nesta mesma execução (`--dry --force`, `MAX_PER_SEG=8`, piso da curva) | **34** (soma de todos os 20 nichos) |
| Nichos que bateram o piso `MIN_SEG=4` e geraram campanha | **2 de 20** (`marketing`: 4, `consultoria`: 4) |

O número que engana é o `4.232` (corporativo nunca-contactado, total): parece bastante estoque, mas
a rotação não busca "qualquer lead nunca-contactado" — busca por **termo de nicho** (`search_terms`
de cada um dos 20 segmentos do `COPY_BANK`), e a maioria dos 4.232 leads não bate com nenhum desses
termos ou está distribuída tão fino entre os 20 nichos que fica abaixo do piso `MIN_SEG=4` por
segmento (ex.: nesta execução: `construtora: 0`, `advocacia: 0`, `otica: 1`, `medico: 1`). Mesmo no
degrau **mais baixo** da curva (dia 1, alvo 87/dia, `MAX_PER_SEG=8`), só 34 dos 4.232 leads
corporativos nunca-contactados eram de fato roteáveis — o motor de rampa está correto e funcional,
mas entregaria muito abaixo do alvo por pura escassez de lead segmentável, não por limitação de
código.

**Quantos dias de cadência o estoque sustenta** (cálculo explícito, sem arredondar para cima):

- No alvo do dia 1 (87/dia), usando o total bruto corporativo nunca-contactado (4.232) **como se
  fosse todo roteável** (não é — ver acima): `4.232 / 87 ≈ 48,6 dias`. Usando o número real
  alcançável nesta execução (34 leads corporativos/dia, teto do que a rotação de nichos consegue
  hoje): **menos de 1 dia** — o pool de 34 leads roteáveis se esgota na primeira execução real, e o
  próximo dia depende inteiramente de reposição.
- No alvo final (300/dia): `4.232 / 300 ≈ 14,1 dias` no cenário otimista (bruto, não roteável);
  no cenário real (34/dia roteável), o alvo de 300/dia é hoje **inatingível** — não é uma questão de
  tempo, é uma questão de distribuição de estoque por nicho.
- Webmail (cap 15/dia, população própria): `719 / 15 ≈ 47,9 dias` de cadência no cap — este é o
  número mais próximo de "confiável" porque a wave de webmail não segmenta por nicho (busca a página
  geral quando a busca por termo não rende, ver §5).

**Fora do escopo deste plano** (§9 detalha): `replenish_leads.py` (reposição via Tavily, já rodado às
sextas em `daily_waves.py main()`) e a contaminação de scraping já registrada nas Phases 9/10 são os
fios a puxar para resolver a causa raiz — este plano apenas mede e documenta, não repõe estoque nem
muda a lógica de roteamento por nicho.

---

## 4. A/B de subject (EMAL-03)

A variante nasce por **alternância por dia**: `daily_waves.build_waves()` lê `st['ab_flip']`
(booleano em `previews/daily-state.json`), decide `variant = 'B' if ab_flip else 'A'`, inverte o
booleano, e sufixa o nome da campanha com `auto s{variant}` (ex.:
`AQ - marketing - Presenca Digital - 2026-09-20 auto sA`). Isso já existia desde a Phase 10 — o que
faltava, e o que `ab_report.py` entrega, é **ler** o resultado.

`ab_report.py` agrupa `email_queue_items`/`email_events` pelo sufixo `s[AB]` do nome da campanha e
calcula taxa de clique por variante. **A métrica vencedora é clique, nunca abertura** (D-05, travada
pelo usuário): opens são afetados pelo Apple Mail Privacy Protection (que pré-carrega o pixel de
tracking independente do usuário abrir o email de verdade), e o próprio
`BrevoWebhookController.cs` do backend já não avança funil por abertura, pelo mesmo motivo. Gate de
amostra mínima: `MIN_CLICKS = 30` cliques totais (soma das duas variantes) antes de declarar um
vencedor; `MIN_DELTA = 0.20` (20% de diferença relativa mínima) para não declarar vencedor por ruído.
Ambos discricionários, documentados no docstring do próprio módulo (`CONTEXT.md` deixou a critério de
implementação).

**Onde o resultado aparece:** resumo de sexta-feira do Telegram (`ab_report.format_tg(...)`, ligado
em `daily_waves.main()` desde o 12-04) e `previews/ab-report.json` (gravado por
`python ab_report.py --write`, consultável fora do Telegram a qualquer momento).

**Veredito medido hoje (30 dias, reproduzido ao vivo nesta execução):** `INCONCLUSIVO`, motivo "12
cliques no total < 30 mínimo para decidir" (`sA`: 88 enviados/2 cliques; `sB`: 270 enviados/10
cliques). Honesto e esperado dado o volume atual — não uma falha do mecanismo.

**Limitação metodológica registrada:** A e B nunca dividem o mesmo dia (a alternância é diária, não
por lead), então parte de qualquer diferença observada entre as variantes é efeito de dia/nicho, não
puramente de subject. Registrado no 12-03-SUMMARY.md, não corrigido (fora de escopo mudar a lógica de
alternância).

**Achado adicional herdado do 12-03, ainda válido:** 61,8% dos envios de 30 dias (578/936) vêm de
campanhas `Outreach Cold - <data> <hora>`, um fluxo separado (`OutreachController`) que nunca recebeu
o sufixo `auto s[AB]` — `ab_report.py` mede corretamente 100% do que `daily_waves.py` envia, mas isso
é hoje só ~38% do volume total de email da conta. Se o Outreach Cold deve entrar no A/B é decisão de
escopo futuro.

---

## 5. Wave de webmail (EMAL-04)

`build_webmail_wave()` é um caminho **paralelo**, não a remoção do filtro `FREE_WEBMAIL` (Pitfall 5
da pesquisa da Phase 12 — misturar webmail com corp destruiria a capacidade de medir a taxa de
resposta de cada população separadamente, que é exatamente o que EMAL-04 pede). O portão
`email_ok(..., mode='corp'|'webmail')` em `waves_core.py` inverte a lógica: `mode='corp'` (default,
usado pelos 20 nichos, byte-a-byte idêntico ao comportamento anterior à Phase 12) rejeita domínios em
`FREE_WEBMAIL`; `mode='webmail'` (usado só por `build_webmail_wave`) aceita **exclusivamente**
domínios em `FREE_WEBMAIL` e desliga a checagem de duplicata-por-domínio (senão o segundo lead gmail
do dia morreria como "dup-dominio").

**`WEBMAIL_CAP = 15`/dia**, rastreado em `state['webmail_sent']` (chave por data, com poda automática
de 30 dias). Meio da faixa 10-20 sugerida pelo `CONTEXT.md` — conservador, porque domínio webmail tem
reputação pior e a base já está com bounce acima de 10%. A campanha nasce com marca
`AQ - webmail - Presenca Digital - {data} auto wm` — o sufixo `auto wm` (em vez de `auto sA`/`sB`)
garante que `ab_report.parse_variant()` devolve `None` para ela: a população webmail fica **fora do
A/B por construção**, não por acidente.

**Contagem viva de leads webmail nunca-contactados (24/24 domínios de `FREE_WEBMAIL`), medida nesta
execução:** 719 — igual ao valor medido no 12-04 (não mudou porque nenhuma campanha real de webmail
rodou desde então). Compara com dois outros números que circulam no projeto e que **não** valem:
`REQUIREMENTS.md` cita 560 (estático, data de coleta desconhecida); `12-RESEARCH.md` estimava ~712
(aproximado, só 17/24 domínios). O valor correto é o medido agora: **719**.

**Como ler a taxa de resposta da população webmail isoladamente:** filtrar campanhas por
`c.name LIKE '% auto wm'` nas mesmas consultas de `email_queue_items`/`email_events` que
`ab_report.py`/`weekly_report()` já usam para o resto do motor — nenhuma tabela nova, é a mesma marca
que distingue o restante do funil.

---

## 6. DMARC (EMAL-01)

Resumo curto — o detalhe completo (regra de avanço/reversão, drill provado em produção, saúde medida,
cadência dos degraus restantes, limites, reversão manual) vive em **`DMARC-escalonamento.md`**, não é
duplicado aqui:

- `dmarc_escalation.py` decide `advance`/`hold`/`revert` por uma função pura (`decide()`), lendo saúde
  real de `email_events` (bounce/spam por `event_type` 4/5 — nunca o agregado `UnsubscribeCount`, que
  mistura spam com opt-out voluntário).
- Toda escrita é gated por `--apply` (nunca acionado fora de um drill contra registro descartável).
- Estado real hoje: **`pct=25`**, segurado deliberadamente — o bounce rate medido (5,08% em 7 dias,
  10,58% em 30 dias) já excede o próprio limiar de reversão `D-04` (>5%) que o usuário travou, então
  subir o degrau contrariaria a regra de segurança que ele mesmo definiu. Isso não é uma falha do
  mecanismo, é o mecanismo funcionando como projetado.
- Próximo passo manual: a partir de 2026-09-27 (7 dias após o `dmarc_changed_at` semeado no 12-05),
  rodar `python dmarc_escalation.py --check` para reavaliar.

Ver `docs/email-marketing/DMARC-escalonamento.md` para o passo a passo completo do drill e a tabela
de cadência dos degraus restantes (25→50→75→100).

---

## 7. Como verificar que tudo está de pé

Os 7 comandos abaixo foram rodados ao vivo nesta execução (2026-09-20), com a saída real reproduzida
verbatim (resumida onde muito longa):

```
$ python waves_smoke.py
OK rotation_coberta
OK copy_bank_integro
OK template_v2_identico
OK healthy_mutavel_in_place
OK imports_locais_resolvem
OK warmup_curva
OK modo_webmail
== 7/7 OK ==

$ python ab_report.py --selftest
OK: veredito INCONCLUSIVO (25 cliques < 30)
OK: veredito sA vence (40 cliques >= 30; 7.5% vs 2.5%)
OK: veredito EMPATE (diferenca relativa < 20%)
OK: veredito SEM-DADOS (sent=0 nas duas variantes)
OK: click_rate(400 sent, 20 clicks) == 0.05
OK: click_rate(sent=0) == 0.0 sem ZeroDivisionError
OK: parse_variant reconhece sufixo 'auto sB'
OK: parse_variant devolve None para campanha webmail ('auto wm')
OK: parse_variant devolve None para campanha legada sem sufixo

$ python dmarc_escalation.py --selftest
OK: TPL.format(pct=50) produz a string DMARC exata
OK: parse_pct extrai pct=25 de um txtdata valido
OK: parse_pct devolve None em string sem pct=
OK: bounce 12% pct=50 -> revert para 25
OK: bounce 12% pct=25 -> hold no minimo
OK: spam 0.2% bounce 0% -> revert
OK: 3 dias -> hold citando dias
OK: 8 dias sent=150 -> hold por amostra
OK: 8 dias sent=500 pct=25 -> advance para 50
OK: pct=100 saude limpa -> done

$ python dmarc_escalation.py --status
Zona WHM: pct=25 (Line 125)
  txtdata: v=DMARC1; p=quarantine; pct=25; rua=mailto:rua@dmarc.brevo.com; adkim=r; aspf=r
State file: dmarc_pct=25 dmarc_changed_at=2026-09-20
  Divergencia zona vs state: nenhuma

$ python dmarc_escalation.py --check
acao=hold pct_atual=25 pct_alvo=25
motivo: faltam 7 dias para o proximo degrau (0/7 dias)

$ python ab_report.py --days 30 --json
{"janela_dias": 30, "veredito": "INCONCLUSIVO", "motivo": "12 cliques no total < 30 minimo para decidir", ...}

$ python daily_waves.py --dry --force
WARMUP dia 1/8 — alvo 87/dia · 4 provider(s) · MAX_PER_SEG=8
DRY: alvo do dia = 87 (warmup dia 1)
DRY: plano = [('marketing', 4), ('consultoria', 4)]
WEBMAIL: 3 descartado(s) (role-mailbox/estrangeiro)
DRY: webmail = 12 leads (cap 15, já hoje 0)
===== FIM =====
```

**As quatro linhas de prova de que os três mecanismos convivem na mesma execução** (warmup + plano +
webmail, tudo no mesmo `--dry --force`):

```
WARMUP dia 1/8 — alvo 87/dia · 4 provider(s) · MAX_PER_SEG=8
DRY: alvo do dia = 87 (warmup dia 1)
DRY: plano = [('marketing', 4), ('consultoria', 4)]
DRY: webmail = 12 leads (cap 15, já hoje 0)
```

Saída esperada de cada comando (para quem for reconferir depois): `waves_smoke.py` → `7/7 OK`;
`ab_report.py --selftest` e `dmarc_escalation.py --selftest` → exit 0, todas as linhas prefixadas
`OK:`; `--status`/`--check` → sem traceback, mostra `pct` real e ação (`hold`/`advance`/`revert`);
`--days 30 --json` → JSON válido com chave `veredito`; `daily_waves.py --dry --force` → chega em
`===== FIM =====` sem traceback, contém as quatro linhas acima.

---

## 8. Limites conhecidos

- **Motor depende do notebook do usuário** (Windows Task Scheduler, task `DIAX-Email-Waves-Daily`,
  trigger diário 08:40 BRT com `DaysInterval=1` — o Scheduler não filtra fim de semana, é o próprio
  script que sai cedo em sábado/domingo sem `--force`, D-04 da Phase 10). Ponto único de falha
  conhecido e aceito; os watchdogs de `OBSERVABILIDADE-watchdogs.md` monitoram WAHA/n8n, não este
  motor.
- **`--dry` não exercita escrita de DNS nem enfileiramento real** — todas as provas deste runbook (e
  de todas as SUMMARYs 12-01..12-05) vêm de `--dry --force` ou de leitura pura. A confirmação de uma
  execução real e não-dry é o objeto do checkpoint da Task 2 deste próprio plano.
- **Bug cosmético conhecido, não corrigido neste plano:** em modo `--dry`,
  `st['warmup_target_hoje'] = target` só é atribuído em `build_waves()` **depois** do
  `if DRY: ... return []` (a atribuição fica na linha ~381, o early-return de DRY está na linha
  ~316-323) — então a mensagem de Telegram formatada em `main()`
  (`f'Agendados hoje: ... / alvo {st.get("warmup_target_hoje", "?")}'`) sempre imprime `alvo ?` quando
  rodada com `--dry`, reproduzido ao vivo nesta execução:
  `Agendados hoje: 0 / alvo ? (warmup dia 1/8)`. **Não afeta uma execução real** — o branch `DRY` nunca
  é alcançado fora de `--dry`, `warmup_target_hoje` é setado normalmente antes da mensagem. Registrado
  aqui (não corrigido) porque este plano proíbe explicitamente qualquer alteração de `.py`
  (`git status --porcelain '*.py'` deve sair vazio de mudanças deste plano); o fix é uma linha
  (mover a atribuição de `st['warmup_target_hoje']`/`st['warmup_delivered']` para antes do bloco
  `if DRY`) e fica como candidato trivial para a próxima sessão que tocar `daily_waves.py`.
- **`ab_report.py` depende do padrão de nome da campanha** — só reconhece `auto s[AB]`; qualquer
  campanha criada fora de `daily_waves.build_waves()` (ex.: `Outreach Cold`, ver §4) fica fora da
  medição.
- **Free tier por provider (~75/provider/dia no alvo 300)** é o próximo teto depois desta fase — hoje
  4 providers ativos (`Brevo`, `Mailjet`, `Resend`, `ElasticEmail`; `SendGrid` fora da rotação por
  auto-heal nesta execução), então 300/dia ÷ 4 ≈ 75/provider/dia já bate perto de limites de free tier
  típicos — não verificado neste plano, fica registrado como candidato de investigação futura.
- **Bounce rate atual (5,08%/7d, 10,58%/30d) acima do limiar de reversão do DMARC (>5%)** — ver §6 e
  `DMARC-escalonamento.md`. É o mesmo sinal de saúde de base que também limita o avanço de EMAL-01.

---

## 9. Próximos fios

Lista curta e priorizada do que esta fase deliberadamente não fez:

1. **Cadência semanal automática do DMARC** — hoje `--check` é manual; um piggyback em
   `daily_waves.py` (ex.: rodar às sextas, junto do `weekly_report()`/`niche_refresh()`) automatizaria
   a reavaliação dos 7 dias sem depender de alguém lembrar de rodar o comando.
2. **Qualidade de lead na entrada (causa raiz do bounce)** — o bounce rate que segura o degrau do
   DMARC não se resolve escalonando volume; resolve-se filtrando melhor na entrada (mesma linha de
   investigação já registrada nas Phases 9/10 sobre contaminação de scraping).
3. **Reposição de estoque por nicho** — `replenish_leads.py` (Tavily, sextas) já existe e já roda,
   mas §3 mostra que o gargalo é distribuição por nicho, não volume total; vale medir se a reposição
   está de fato repondo os nichos escassos ou só engordando os que já têm estoque.
4. **Amostra do A/B** — ao ritmo atual (~0,4 clique/dia), faltam ~45 dias adicionais para atingir
   `MIN_CLICKS=30` e permitir um veredito diferente de `INCONCLUSIVO`. Esse prazo cai se o warmup de
   volume (EMAL-02) conseguir aumentar o volume diário de fato — mas isso depende de §3 ser resolvido
   primeiro.
5. **Bug cosmético do `alvo ?` em `--dry`** (§8) — trivial, mas não corrigido aqui por proibição
   explícita de tocar `.py` neste plano.

---

*Phase: 12-escala-de-email*
*Runbook consolidado gerado em: 2026-09-20 (plano 12-06)*
*Ponteiros: `DMARC-escalonamento.md` (EMAL-01), `OBSERVABILIDADE-watchdogs.md` (pipeline fora do
notebook, Phase 10), `QUALIFIED-avanco-automatico.md` (avanço automático de funil, Phase 11)*
