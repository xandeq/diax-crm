# Migração dos scripts `fire_*.py` — caminho documentado (SCRP-01)

Gerado em: 2026-09-19 (Phase 10, Plano 10-01 — inventário gerado por comando, não por memória)

---

## 1. Por quê

`SCRP-01` (critério 2 de sucesso da fase 10: "os 79+ scripts têm caminho de migração
documentado") **não pede um motor novo**. O achado real do research (10-RESEARCH.md, D-01) é
que `daily_waves.py` **já é** o motor único parametrizado — roda seg-sex 08:40 BRT via Task
Scheduler, monta até 3 ondas de 4 segmentos, follow-up multi-toque (FUP1/2/3), circuit breaker,
filtro de lead estrangeiro/role-mailbox e digest de hot-leads no Telegram.

O trabalho desta fase é:
1. Extrair as funções/constantes que `daily_waves.py` hoje importa de 3 arquivos `fire_*.py`
   para um módulo próprio (planos 10-02/10-03) — não reescrever a lógica.
2. Arquivar (não apagar) os `fire_*.py` que já cumpriram seu papel de campanha pontual — plano
   10-04, só depois que os 7 dependentes vivos listados na seção 3 estiverem religados e
   verificados.

Este documento é o mapa de quem depende de quem, gerado agora, antes de qualquer arquivo ser
movido.

---

## 2. Mapa de extração

`daily_waves.py` importa hoje (linhas 21-24) de 3 arquivos-fonte:

| Nome | Vinha de | Passa a viver em |
|------|----------|-------------------|
| `DIR`, `base`, `HEALTHY`, `PER_PROVIDER`, `FREE_WEBMAIL`, `FOREIGN_TLD`, `ts`, `load_env`, `login`, `qp`, `ensure_breaker_closed`, `build_html`, `gen_image`, `email_ok`, `_mx_deliverable`, `fetch_leads`, `assign_providers` | `fire_today_20.py` | `waves_core.py` (módulo novo, planos 10-02/10-03) |
| `build_html_v2`, `SEGMENTS` | `fire_w1_2707.py` | `waves_copy.py` (módulo novo) |
| `WAVES` | `fire_w2w3_2707.py` | `waves_copy.py` |

### Restrição obrigatória (Pitfall do research — não separar)

`HEALTHY`, `PER_PROVIDER` e `assign_providers` **têm de ficar no MESMO módulo**. Hoje,
`daily_waves.refresh_healthy()` faz:

```python
import fire_today_20 as ft
ft.HEALTHY[:] = healthy   # in-place: assign_providers usa a mesma lista
```

Isso funciona porque `assign_providers` (definida dentro de `fire_today_20.py`) resolve
`HEALTHY` contra os *globals do próprio módulo* em tempo de chamada — e `ft.HEALTHY[:] = healthy`
é slice-assignment (muta o objeto lista, não rebind). Se `HEALTHY` for extraída para um módulo e
`assign_providers` para outro, a mutação in-place desincroniza em silêncio: `refresh_healthy()`
continuaria "funcionando" sem erro, mas `assign_providers` nunca veria os providers atualizados.

`waves_smoke.py` (10-01, plano anterior a este) tem um check dedicado a essa restrição
(`healthy_mutavel_in_place`, resolvendo o módulo dono via
`sys.modules[daily_waves.assign_providers.__module__]`) — ele é agnóstico de qual módulo acaba
hospedando `HEALTHY`, justamente para sobreviver a este refactor.

---

## 3. Os 7 dependentes vivos (ordem obrigatória — D-03)

Confirmado agora por `grep -rn "from fire_\|import fire_" --include=*.py .` em
`docs/email-marketing/` — os 7 arquivos abaixo **não têm nome `fire_*`** e **não estão mortos**,
apesar de importarem de `fire_today_20.py`:

| Arquivo:linha | Importa | O que faz | Observação |
|---------------|---------|-----------|------------|
| `debug_campaign.py:31` | `import fire_today_20 as ft` (alias de módulo) | Ferramenta manual de debug/diagnóstico de campanha | **NUNCA pode ser importado por um teste** — linhas 4-37 rodam no nível do módulo: lê secrets, faz login e cria campanha de e-mail real via POST. `waves_smoke.py` só faz `py_compile`/AST nele, nunca `import debug_campaign`. |
| `import_extrator_bridge.py:23` | `load_env, login, base` | **Ponte extrator→CRM de produção** — pipeline crítico que o usuário perguntou explicitamente se continua funcionando | Maior risco da fase. STATE.md registra como "fallback/paralelo" ao `ExtractorPullWorker` (.NET) — não agendado no Task Scheduler, mas não é código morto. |
| `replenish_leads.py:13` | `load_env, login, base, ts` | Reposição automática de estoque de leads | **Chamado dinamicamente por `daily_waves.py` (~linha 779)**, toda sexta, dentro de um `try/except Exception`. Uma quebra de import aqui **não derruba a run** — vira só um aviso no Telegram ("ESTOQUE BAIXO e reposição automática FALHOU") e pode passar semanas despercebida. |
| `rescrape_21.py:18` | `load_env, login, base, ts` | Utilitário ad-hoc de rescrape | Não agendado. |
| `rescrape_27.py:17` | `load_env, login, base, ts` | Utilitário ad-hoc de rescrape | Não agendado. |
| `rescrape_tavily_27.py:11` | `load_env, login, base, ts` | Utilitário ad-hoc de rescrape via Tavily | Não agendado. |
| `scrape_and_import.py:16` | `load_env, login, base, ts` | Utilitário ad-hoc de import de scraping | Não agendado. |

**Regra registrada: nenhum `fire_*.py` vai para `archive/` antes desses 7 estarem religados e
verificados** — a atualização dos imports desses 7 arquivos para o módulo novo (`waves_core.py`)
é o plano **10-03**, e só depois vem o arquivamento físico (plano **10-04**). Decisão do usuário
em CONTEXT.md (D-03): atualizar os imports diretamente, sem shim de compatibilidade.

---

## 4. Critério de arquivamento

O critério é **padrão de nome de arquivo** (`fire_*.py`), **não status no git**. A maioria dos
`fire_*.py` está untracked, mas untracked não significa morto — `reply_watch.py`, por exemplo, é
tracked e está bem vivo (trabalho em andamento, ver STATE.md/Blockers).

`ls fire_*.py` em `docs/email-marketing/` retorna **48 arquivos** (confirmado agora, 2026-09-19):

```
fire_lote34.py       fire_w1_2707.py      fire_w3_20.py        fire_w5_25.py        fire_w7_27.py
fire_today_19.py     fire_w2_19.py        fire_w3_21.py        fire_w5_27.py        fire_w8_20.py
fire_today_20.py     fire_w2_20.py        fire_w3_24.py        fire_w6_20.py        fire_w8_24.py
fire_w10_20.py       fire_w2_21.py        fire_w3_25.py        fire_w6_21.py        fire_w8_25.py
fire_w11_20.py       fire_w2_24.py        fire_w3_27.py        fire_w6_24.py        fire_w8_27.py
fire_w1_21.py        fire_w2_25.py        fire_w4_20.py        fire_w6_25.py        fire_w9_20.py
fire_w1_24.py        fire_w2_27.py        fire_w4_21.py        fire_w6_27.py        fire_wave_today.py
fire_w1_25.py        fire_w2w3_2707.py    fire_w4_24.py        fire_w7_20.py
fire_w1_27.py        fire_w3_19.py        fire_w4_25.py        fire_w7_24.py
                                           fire_w4_27.py        fire_w7_25.py
                                           fire_w5_20.py
                                           fire_w5_21.py
                                           fire_w5_24.py
```
(gerado via `ls -1 fire_*.py | sort`; 48 nomes ao todo, reformatado em colunas só para leitura)

Dos 48, **3 são fonte de extração** e vão para `archive/` **verbatim, sem edição**, como
snapshot histórico do estado pré-refactor (o mesmo estado que `waves-baseline.json` congelou):

- `fire_today_20.py`
- `fire_w1_2707.py`
- `fire_w2w3_2707.py`

Os outros **45** são campanhas pontuais já disparadas (`fire_w1_21.py` ... `fire_w11_20.py`,
`fire_lote34.py`, `fire_today_19.py`, `fire_wave_today.py`, etc.) — nenhum deles aparece como
alvo de `from fire_X import` em nenhum outro arquivo da árvore (confirmado pelo grep da seção 3:
os únicos alvos de import são `fire_today_20` e `fire_w1_2707`). São candidatos a arquivamento
assim que os 3 arquivos-fonte forem extraídos e os 7 dependentes vivos religados.

---

## 5. Como criar uma campanha nova a partir de agora

**Não copiar um `fire_*.py`.** O padrão antigo (duplicar um script inteiro por campanha) é
exatamente o que esta fase substitui. A partir de agora:

1. Adicionar o nicho em `waves_copy.SEGMENTS`/`WAVES` (pós-extração; hoje ainda em
   `fire_w1_2707.SEGMENTS`/`fire_w2w3_2707.WAVES`) — ou inline em `daily_waves.COPY_BANK`, como
   já é feito para 8 dos 20 nichos atuais.
2. Incluir o nome do nicho na `ROTATION` de `daily_waves.py`.
3. Validar com `python waves_smoke.py` (5s, offline) e depois
   `python daily_waves.py --dry --force` (com rede, não envia nada — `--force` ignora a guarda
   de fim de semana).

---

## 6. Como verificar que nada quebrou

Dois comandos, nesta ordem:

```bash
python waves_smoke.py                    # offline, ~5s: rotation/COPY_BANK/template hash/HEALTHY/imports
python daily_waves.py --dry --force      # com rede, não envia nada; confirma o plano completo de 20 nichos
```

`waves_smoke.py` (criado neste mesmo plano, 10-01) é a rede de segurança rápida — hash-freeze
contra `waves-baseline.json`, gerado ANTES desta extração começar. `--dry --force` é a
confirmação funcional completa, incluindo os 12 nichos que só existem hoje via `SEGMENTS`/`WAVES`
(pitfall 2 do research: extração incompleta não dá `ImportError`, produz `COPY_BANK` menor e
falha só quando o cursor da rotação chegar no nicho quebrado — dias depois).

---

*Documento gerado como parte do plano 10-01 (Phase 10 — Motor de Cadência Único). Inventário via
`ls fire_*.py` e `grep -rn "from fire_\|import fire_" --include=*.py .`, não por memória.*
