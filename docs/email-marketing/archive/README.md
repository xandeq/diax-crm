# Archive dos scripts `fire_*.py` — histórico, NÃO executável

**Arquivado em:** 2026-09-19 (Phase 10, Plano 10-04 — SCRP-01)

---

## Por quê

Estes 48 arquivos são o script sprawl que a Phase 10 (Motor de Cadência Único) consolidou.
Cada `fire_*.py` foi escrito para disparar UMA campanha pontual em UMA data específica de 2026 —
o padrão era "duplicar um script inteiro por campanha nova". Esse padrão foi substituído pelo
motor único parametrizado: `daily_waves.py` (roda seg-sex 08:40 BRT via Task Scheduler) + os
módulos `waves_core.py` (infra: login, breaker, envio, healthy providers) e `waves_copy.py`
(template/copy/segmentos), extraídos verbatim dos 3 arquivos-fonte nos planos 10-02/10-03.

Ver o mapa completo de extração (quem virou o quê) em
[`../MIGRACAO-fire-scripts.md`](../MIGRACAO-fire-scripts.md).

---

## ⚠️ AVISO: conteúdo histórico — NÃO EXECUTAR

Estes scripts foram escritos para datas/campanhas específicas de 2026 e **fazem POST em
produção** (login real, criação de campanha de e-mail real via Brevo/Mailjet/etc.) se rodados
hoje. Rodar qualquer um deles agora:

- Vai tentar reenviar para leads já contactados naquela campanha antiga (sem cooldown/dedupe do
  motor atual).
- Vários (`fire_w1_21.py`, `fire_w2_19.py`, etc.) importam `fire_today_20` / `fire_w1_2707` como
  módulo (`import fire_today_20 as ft`) assumindo que estão no **mesmo diretório** do
  interpretador. Agora que estão dentro de `archive/`, esses imports relativos **não resolvem
  mais** (`archive/` não está no `sys.path` dos scripts vivos) — mesmo tentando rodar um deles
  isoladamente a partir daqui, o comportamento não é garantido a ser idêntico ao de quando
  rodavam na raiz.

Se precisar consultar o que uma campanha antiga fez: leia o código, não execute.

---

## De onde veio o código vivo (símbolo → módulo novo)

| Símbolo | Vinha de (aqui em `archive/`) | Vive hoje em |
|---|---|---|
| `DIR`, `base`, `HEALTHY`, `PER_PROVIDER`, `FREE_WEBMAIL`, `FOREIGN_TLD`, `ts`, `load_env`, `login`, `qp`, `ensure_breaker_closed`, `build_html`, `gen_image`, `email_ok`, `_mx_deliverable`, `fetch_leads`, `assign_providers` | `fire_today_20.py` | `waves_core.py` |
| `build_html_v2`, `SEGMENTS` | `fire_w1_2707.py` | `waves_copy.py` |
| `WAVES` | `fire_w2w3_2707.py` | `waves_copy.py` |

Mapa completo, com evidência (md5, `inspect.getsource`, hash de template) em
[`../MIGRACAO-fire-scripts.md`](../MIGRACAO-fire-scripts.md).

Os outros 45 arquivos aqui dentro eram campanhas pontuais já disparadas (`fire_lote34.py`,
`fire_today_19.py`, `fire_wave_today.py`, `fire_wN_XX.py`) — nenhum deles era importado por
nenhum script vivo (confirmado por grep antes do arquivamento).

`fire_today_20.py`, `fire_w1_2707.py` e `fire_w2w3_2707.py` foram movidos para cá **verbatim, sem
edição** — snapshot histórico do estado pré-refactor, byte-a-byte idênticos ao arquivo original
(md5 conferido antes e depois do move; ver `10-04-SUMMARY.md`), mantidos aqui só para consulta e
como referência de proveniência do código que hoje vive em `waves_core.py`/`waves_copy.py`.

---

## Como criar uma campanha nova hoje (não copiar um script daqui)

1. Adicionar o nicho em `waves_copy.SEGMENTS`/`WAVES`, ou inline em `daily_waves.COPY_BANK`.
2. Incluir o nome do nicho na `ROTATION` de `daily_waves.py`.
3. Validar com `python waves_smoke.py` (offline, ~5s) e depois
   `python daily_waves.py --dry --force` (com rede, não envia nada).

Nenhuma campanha nova exige duplicar um arquivo `fire_*.py`.
