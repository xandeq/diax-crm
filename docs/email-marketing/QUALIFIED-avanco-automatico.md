# Qualified — Critério Objetivo e Avanço Automático (QUAL-01/QUAL-02)

Gerado em: 2026-09-20 (Phase 11, plano 11-01)

Escopo travado por `11-CONTEXT.md` (D-01): **verificar e documentar**, não construir. A automação já
existe em produção desde 2026-09-04 — este runbook prova que ela funciona de verdade e registra os
limites conhecidos. Nenhuma linha de `docs/email-marketing/reply_watch.py` foi alterada para produzir
este documento (guard MD5 antes/depois na seção 3 confirma isso).

---

## 1. Critério objetivo (QUAL-01)

Um lead avança de **Contacted (status 1)** para **Qualified (status 2)** quando os primeiros 1500
caracteres da resposta de e-mail do lead **NÃO casam com a regex `NEG`** — ou seja:

```
classify_intent(texto) != 'negative'
```

Respostas `positive` **e** `neutral` contam como avanço. Fonte: `docs/email-marketing/reply_watch.py`,
linhas 28-31 (regexes) e 59-63 (`classify_intent`), verbatim:

```python
# Source: docs/email-marketing/reply_watch.py, linhas 28-31 e 59-63
NEG = re.compile(r'n[ãa]o (tenho|temos|tem|quero|queremos|envie|enviem|mande|mandem|precis)|sem interesse|remov|descadastr|'
                 r'retir(ar|em)|pare de|parem de|unsubscribe|spam|não me envie|nao me envie', re.I)
POS = re.compile(r'pode (enviar|mandar|explicar|ligar|chamar)|tenho interesse|temos interesse|interess|or[çc]amento|'
                 r'quanto (custa|fica|é|e o valor)|valor|pre[çc]o|vamos conversar|me liga|me chama|agend|proposta|'
                 r'gostaria de|quero (saber|entender|ver)', re.I)

def classify_intent(text):
    t = (text or '')[:1500]
    if NEG.search(t): return 'negative'
    if POS.search(t): return 'positive'
    return 'neutral'
```

Conferido nesta sessão (2026-09-20) contra o arquivo real em disco — bate byte-a-byte com o bloco
`<interfaces>` do plano 11-01.

**A frouxidão do critério é conhecida e mantida de propósito.** "Não-negativo = Qualified" aceita
resposta neutra tipo "quem é você?" ou "recebi seu email, obrigado" — não exige interesse explícito.
Endurecer esse critério (por exemplo, exigir match em `POS`) ficou **adiado por decisão do usuário
(D-01)**: não é escopo desta fase.

Status enum de referência (`CLAUDE.md`): `0=Lead, 1=Contacted, 2=Qualified, 3=Negotiating, 4=Customer`.

## 2. Onde e como roda (QUAL-02)

O avanço de status acontece dentro de `main()`, linhas 279-283 de `reply_watch.py`, verbatim:

```python
# Source: docs/email-marketing/reply_watch.py, main(), linhas 279-283
crm_task(H, title, desc, priority=4 if intent != 'negative' else 2)
if intent == 'negative':
    suppress(em)
elif lead.get('customer_id') and (lead.get('status') or 0) < 2:
    crm_status(H, lead['customer_id'], 2)
```

Mecanismo:
- `crm_status(H, cid, status)` faz `PATCH /api/v1/customers/{id}/status {"status": 2}` — **mesmo
  endpoint** que `mcp__diax-crm__update_lead_status` usa.
- O ramo `negative` chama `suppress(em)` (INSERT direto em `email_suppressions` via `db.py`,
  pyodbc) e **nunca** avança status.
- O guard `lead.get('status') or 0) < 2` evita PATCH redundante num lead que já passou de Qualified
  (evita retroceder Negotiating/Customer para Qualified).

### Task agendada `DIAX-Reply-Watch` — reconferida ao vivo nesta sessão (2026-09-20)

| Campo | Valor (reconferido 2026-09-20) |
|---|---|
| `State` | `Ready` |
| `LastRunTime` | 9/18/2026 7:00:01 PM |
| `LastTaskResult` | `0` (sucesso) |
| `NextRunTime` | 9/21/2026 8:00:00 AM (pula o fim de semana corretamente — hoje é domingo) |
| `NumberOfMissedRuns` | `0` |
| `StartBoundary` | `2026-09-04T08:00:00-03:00` |
| `DaysOfWeek` | `62` (seg-sex) |
| Repetição | `Interval=PT30M`, `Duration=PT11H` (08:00→19:00 BRT) |
| Ação | `C:\Program Files\Python314\pythonw.exe D:\claude-code\diax-crm\docs\email-marketing\reply_watch.py` — **sem `--dry`** |
| `WorkingDirectory` | `D:\claude-code\diax-crm\docs\email-marketing` |

Nenhuma divergência encontrada em relação ao esperado (pesquisa de 2026-09-20) — `State=Ready`,
`LastTaskResult=0`, cadência e ação idênticas às documentadas.

Caminhos de estado/log:
- `docs/email-marketing/previews/replies-state.json` (`last_uid`/`seen`, persistido só fora de `--dry`)
- `docs/email-marketing/logs/reply-watch.log` (heartbeat + classificações, gravado sempre)

## 3. Prova real de funcionamento (D-02)

**Caso confirmado:** `atendimento@listamais.com.br`, customer_id
`2dcb6a14-006a-426c-bc60-b6599a8f0e64` ("Pet Shop Abertos 24 Horas Em Fundão, Es").

Tabela de correlação (reconferida ao vivo nesta sessão via
`GET /api/v1/audit-logs/resource/Customer/2dcb6a14-006a-426c-bc60-b6599a8f0e64`):

| Evento | Horário |
|---|---|
| Log `reply_watch.py`: `[2026-09-07 08:00:08] 🟢 atendimento@listamais.com.br [positive] pet` | BRT (UTC-3) |
| Mesmo instante em UTC | `2026-09-07 11:00:08 UTC` |
| Audit log do CRM: `Status: 1 → 2`, `changedProperties` inclui `Status` (entidade inteira) | `2026-09-07T11:00:22.9668226Z` |
| Diferença | **14 segundos** |

14 segundos é exatamente compatível com a sequência síncrona dentro da mesma execução de `main()`:
`api_login()` (POST) → `crm_task()` (POST) → `crm_status()` (PATCH). Este é o evento de audit log
confirmado (`oldValues.Status=1`, `newValues.Status=2`), reconferido nesta sessão — não copiado da
pesquisa sem checar.

**Caminho negativo (prova de ausência de falso positivo)** — reconferido ao vivo nesta sessão:

| E-mail | Intent classificado | Status atual (CRM) | Evento de audit log que altere `Status`? |
|---|---|---|---|
| `coordenacao@atomclinica.com.br` | `negative` | `1` (Contacted) | **Nenhum** — só `LeadScore`/`Segment` (backfill Phase 9) |
| `recepcao@vilaaltodospinhais.com.br` | `negative` | `1` (Contacted) | **Nenhum** — só `LeadScore` (backfill Phase 9) |

Ambos os leads classificados `negative` permanecem em Contacted, sem nenhum PATCH de `Status` em seu
histórico de auditoria — consistente com o ramo `negative` chamar `suppress()` em vez de
`crm_status()`. Isso satisfaz o Critério 4 de sucesso da fase (ROADMAP.md): "um lead que não atinge o
critério... permanece em Contacted".

### Como reproduzir esta prova

```
Get-ScheduledTask -TaskName 'DIAX-Reply-Watch'
Get-ScheduledTaskInfo -TaskName 'DIAX-Reply-Watch'
GET https://api.alexandrequeiroz.com.br/api/v1/customers?search={email}
GET https://api.alexandrequeiroz.com.br/api/v1/audit-logs/resource/Customer/{id}
Authorization: Bearer {admin JWT via POST /api/v1/auth/login}
```
`audit-logs/resource/Customer/{id}` requer permissão `audit.view`, já concedida à conta admin que o
próprio `reply_watch.py` usa em `api_login()`.

**Aviso obrigatório: BRT → UTC, somar 3 horas** antes de comparar timestamps de log (horário local do
notebook, America/Sao_Paulo) contra `timestampUtc` do audit log — senão a correlação parece errada
por 3 horas.

**Aviso anti-armadilha:** `status=2` no CRM **não é prova por si só** de que `reply_watch.py` causou o
avanço. `pedido@bfffitnessatacado.com.br` está `status=2` hoje, mas seu evento `Status: 1→2`
(`2026-09-05T10:04:55Z`) é **anterior** a qualquer log de `reply_watch.py` para esse endereço — esse
lead já estava Qualified por outro caminho antes do script sequer classificar sua resposta (que aliás
foi `neutral`). O guard `status < 2` fez o script corretamente pular um PATCH redundante. Só correlação
de timestamp ao segundo, como no caso `listamais` acima, conta como prova de QUAL-02.

## 4. O que NÃO está coberto (fronteira D-01)

- **WhatsApp**: o canal só cria tarefa/alerta via `crm_task()`/Telegram, nunca avança status
  automaticamente — auto-avanço no WhatsApp está adiado (Deferred Ideas, `11-CONTEXT.md`).
- **Critério frouxo**: "não-negativo = Qualified" aceita resposta neutra sem interesse explícito.
  Endurecer exige decisão do usuário sobre o que conta como interesse real — adiado.
- **IMAP sem watchdog (lacuna de falha silenciosa)**: `fetch_new()` conecta em
  `mail.alexandrequeiroz.com.br:993` usando `HOSTGATOR_CONTATO_EMAIL`/`HOSTGATOR_CONTATO_PASS`. Se a
  credencial expirar ou o servidor de e-mail parar de responder, a função loga `'sem
  HOSTGATOR_CONTATO_*'`, retorna `[]`, e a task continua reportando `LastTaskResult=0` — **sucesso
  aparente fazendo nada**. Os watchdogs da Phase 10 (`n8n-watchdog`, `waha-watchdog`) monitoram n8n e
  WAHA, mas **não** cobrem o servidor de e-mail Hostgator. Registrado como candidato a fase futura,
  **sem propor implementação aqui**.
- **Anomalia aberta, não bloqueante**: `contato@choferbrasilturismo.com.br` aparece no log em
  `2026-09-04 16:53:29` como `🟢 [positive]`, mas o `replies-state.json` final registra
  `"intent": "neutral"` para esse endereço, e o lead nunca saiu do status 1 — nenhum evento de audit
  log de `Status` existe para esse customer. Explicação mais provável (não confirmada): execução
  manual com `--dry` no dia do deploy — `save_state()` só grava `if not DRY`, enquanto `log()` grava
  sempre, o que explicaria log e state divergentes sem persistência real. Segunda hipótese: o PATCH de
  `crm_status()` falhou em silêncio (a função retorna `r.status_code == 200` como bool, mas `main()`
  nunca inspeciona esse retorno). Deixado como pergunta em aberto documentada — **não investigado nem
  corrigido nesta fase**.
- **Tasks API não reverificada de forma independente**: assume-se que `crm_task()` roda junto com
  `crm_status()` (mesmo caminho de código, sequencial, dentro do mesmo `main()`), mas isso não foi
  confirmado via `GET /api/v1/tasks` nesta sessão — marcado como "assumido, não verificado
  independentemente".

## 5. Dependências externas

`reply_watch.py` faz `sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts')` e importa
`db` (como `crmdb`, usado em `suppress()`) e `site_check` (`check_site`/`summarize_fup`) de **fora do
repositório `diax-crm`** — na pasta global de skills do usuário. `db.py` abre uma conexão `pyodbc`
**direta no SQL Server de produção** (`sql1002.site4now.net`), contornando a API .NET inteiramente.

Isso significa que existem **dois caminhos de escrita** no CRM a partir deste script:
1. SQL direto via `db.py` (`suppress()` → INSERT em `email_suppressions`)
2. REST via `crm_status()`/`crm_task()` (`PATCH`/`POST` contra a API .NET)

O script quebra se a pasta global `C:\Users\acq20\.claude\skills\financas\scripts` for reorganizada ou
movida. Isto é registro do estado atual, **não um item de refactor desta fase**.

---

## Nota de manutenção

Esta evidência é um snapshot de 2026-09-20. Quem revalidar este runbook no futuro deve re-rodar os
comandos da seção 3 contra uma entrada **nova** de `replies-state.json` (a próxima resposta real
classificada não-negativa) para confirmar que o mecanismo continua vivo — não basta reler os números
já registrados aqui, eles vão ficar mais velhos a cada semana que passa.
