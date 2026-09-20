# Escala de WhatsApp — Runbook Consolidado (Phase 13, WHAT-01..04)

Gerado em: 2026-09-20 (Phase 13, plano 13-04)

Este documento consolida os quatro mecanismos que a Phase 13 entregou/verificou no canal
WhatsApp: handoff de prospecção em execução real (WHAT-01), deflexão do bot inbound de IA pra
humano (WHAT-02), relatório semanal (WHAT-03) e o motor de decisão wa_es→wa_br (WHAT-04). Segue
o mesmo formato dos runbooks das Phases 10/11/12 (`OBSERVABILIDADE-watchdogs.md`,
`QUALIFIED-avanco-automatico.md`, `ESCALA-EMAIL-runbook.md`): o que é, onde roda, prova, limites,
dependências — sem inventar nada que não tenha sido medido nesta sessão ou nas sessões 13-01/02/03
que a precederam.

---

## 1. Mapa do canal hoje

**Quem envia (prospecção, 1-a-1):** `D:\claude-code\n8n-local\outreach-send.py`, via task Windows
`outreach-wa` (janela 09h-18h, `CAP_DIA=18`, `MAX_PRIO=5`, repetição a cada 30 min por 9h). Travas,
na ordem em que `escolhe()` as aplica:

1. Kill-switch `logs/OUTREACH-STOP` — se existir, nada é enviado.
2. Blacklist é a **primeira** checagem de conteúdo — `logs/outreach-blacklist.json['recusaram']`
   (7 números hoje); blacklist vazia é tratada como bug de leitura, não como realidade, e aborta.
3. `logs/outreach-sent.json` (186 chat_ids all-time) — já contatado nunca recebe de novo.
4. Janela (09h-18h) e `CAP_DIA=18`.
5. Prioridade (`MAX_PRIO=5`) e mensagem não vazia/sem encoding quebrado.

**Gravação verificada:** `grava_verificado()` grava e **relê** o arquivo para confirmar — a lição
direta do incidente de 12/09 (ver §7). `outreach-send.py` marca o `chat_id` como enviado **antes**
de chamar `sendText` (perder um lead é barato; reenviar para quem já recebeu, não).

**Quem vigia respostas:** `outreach-watch.py`, via task `outreach-watch` (repetição a cada 20 min
por 15h). Escreve `logs/outreach-watch-estado.json` (`chat_id|timestamp_unix`) e dispara alerta ao
dono quando há resposta nova (linha `WATCH ALERTA enviado ao dono sobre N lead(s)` no audit log).

**O que o n8n faz e não faz:** o n8n local está **fora do ar** (porta 5678 fechada, task
`n8n-local` `Disabled`, confirmado ao vivo nesta sessão — ver §8). O workflow de prospecção
`waOutreach1a1AQ` permanece `active=0`, `cap=2`, `max_prio=4`, última execução `2026-09-12
17:05:36` — **inalterado desde o 13-01**, por escolha deliberada (ver §2). O bot inbound
`8nKTlX4Y7Hy1EUEn` está `active=1` no banco do n8n, mas **inerte na prática**: o hook do WAHA
aponta para `host.docker.internal:5678`, que não responde enquanto o n8n estiver desligado — ou
seja, hoje NENHUMA mensagem inbound passa pela lógica de deflexão do Guard (§3), porque o próprio
n8n que hospeda essa lógica está fora do ar. Isto é uma lacuna operacional real, registrada aqui
(não é objeto de correção desta fase — WHAT-02 mede o **mecanismo** de sincronização do conjunto
de deflexão, que continua correto e pronto para quando o n8n voltar).

---

## 2. WHAT-01 — handoff em execução

**Decisão do checkpoint (13-01, Task 2):** o usuário respondeu **`python-oficial`** quando
apresentado com o estado ao vivo (workflow `waOutreach1a1AQ` `active=0`, caminho Python já
rodando com segurança). `outreach-send.py`/task `outreach-wa` foi declarado o handoff **oficial**
de WHAT-01. `waOutreach1a1AQ` permanece `active=0`, `cap=2`, `max_prio=4` — desligado de propósito
desde o incidente de 12/09 (Code node do n8n com `fs.writeFileSync`/`readFileSync` não confiáveis
→ `catch` vazio engoliu o erro → dedupe cego → 7ª mensagem para um lead que já havia recusado em
10/09). Zero reativação do n8n foi feita nesta fase.

**Prova de execução real (citada, `previews/whatsapp-estado.json`):** `outreach-sent.json`
all-time = **186 chat_ids**; hoje **16/18** enviados; último `ENVIADO` `2026-09-20 17:30:13
5527996320214@c.us | Cominoti Construções`. Task `outreach-wa`: `Status=Ready`, `Last Result=0`,
`Last Run Time=9/20/2026 6:00:02 PM`, `Next Run Time=9/21/2026 9:00:00 AM`.

**Status na rastreabilidade:** `Complete por equivalência funcional` — o requisito nomeia
literalmente `n8n-wa-outreach-1a1.json`, mas o caminho que de fato roda, com segurança
comprovada (dedupe verificado, blacklist-first, kill-switch), é `outreach-send.py`. Mesmo padrão
já usado em QUAL-01/02 (Phase 11).

---

## 3. WHAT-02 — deflexão pra humano

**Como o nó `Guard` decide (quando o n8n está no ar):** no workflow `8nKTlX4Y7Hy1EUEn`, toda
mensagem inbound é checada contra `s.crmLeads = {chatId: 1, ...}` (staticData persistido no banco
do n8n) **antes** de qualquer chamada de LLM. Se `chatId` está no conjunto: nunca gera resposta de
IA — só classifica `reply`/`optout` (regex de opt-out) e dispara `POST
/api/v1/integrations/whatsapp-event` + notificação "CRM: avisa dono" (WAHA `sendText` para o
próprio número do dono, `5527996302718@c.us` — **não** Telegram, apesar de
`HANDOFF-whatsapp-leads.md` §5 dizer "encaminhar p/ Telegram"; ver §7).

**Como `crm_leads_sync.py` mantém o conjunto:** reescrito no 13-02 de um populador manual de 22
linhas para um sync incremental verificado — `fontes()` (`wa_estado.ler_sent()` +
`ler_blacklist()` + `previews/whatsapp-leads.json`) → `atuais()` (lê `staticData.global.crmLeads`
do sqlite do n8n, **somente-leitura**) → `delta()` → `empurrar()` (POST incremental,
`reset=False`) → `verificar()` (relê o sqlite pós-push; id que não confirmar volta pra fila).

**Task `DIAX-WA-CrmLeads-Sync`:** diária 09:00, repetição a cada 1h por 10h. Confirmada nesta
sessão: `Status=Ready`, `Last Result=0`, `Last Run Time=9/20/2026 6:24:00 PM`, `Next Run
Time=9/20/2026 7:00:00 PM`.

**Comportamento com n8n fora do ar (fila pendente):** confirmado ao vivo agora —
`python crm_leads_sync.py --status` → `fonte=1075 atual=1057 delta=18 pendentes=18
descartados=0`. Os 18 ids do delta desta sessão continuam gravados em
`previews/crmleads-pending.json` (fila durável, nunca sucesso falso) e serão aplicados
automaticamente assim que o n8n voltar ao ar — a task roda de hora em hora sem exigir ação manual.

**Prova do 13-02:** dos 18 ids do delta capturado naquela execução, todos os 18 eram chat_ids que
`outreach-send.py` genuinamente contatou (presentes em `outreach-sent.json`) mas que nunca haviam
entrado em `staticData.global.crmLeads` — ex. `5527998544746@c.us` (AONI IMÓVEIS), enviado em
2026-09-15 09:00:11, ainda fora da deflexão até aquele sync rodar.

**Status na rastreabilidade:** `Complete` — o mecanismo de sync contínuo existe, está agendado, e
provou capturar um gap real (não hipotético).

---

## 4. WHAT-03 — relatório semanal

**Comando:** `python whatsapp_weekly_report.py --dry` (não envia ao Telegram, só calcula e grava).
**Task:** `DIAX-WA-Weekly-Report`, sexta 17:00 BRT — distinta e posterior às 08:40 BRT de
`DIAX-Email-Waves-Daily` (intocada). Confirmada nesta sessão: `Status=Ready`, `Last Result=0`,
`Last Run Time=9/20/2026 6:20:30 PM`, `Next Run Time=9/25/2026 5:00:00 PM` (sexta que vem).

**Exemplo real da mensagem entregue** (rodado nesta sessão, `--dry`, idêntico ao texto que foi
enviado de fato ao Telegram em 13-03):

```
📱 <b>Semana de WhatsApp</b>
Enviados: 111 (ES 111 · BR 0)
Respondidos: 53 (ES 52 · BR 1) — taxa 47.7%
  taxa ES: 46.8% · taxa BR: —
Opt-out: 7 (novos: 0)
CRM registra: 0 enviados — divergencia de 111 (outreach-send.py nao escreve no CRM)
```

**Status na rastreabilidade:** `Complete` — `whatsapp_weekly_report.py` mede exclusivamente via
`wa_estado.py` (nenhuma reimplementação de parsing), a taxa é honesta (denominador zero → `None`,
nunca `0.0%`), e a entrega real no Telegram foi provada em 13-03 (`LastTaskResult=0`).

---

## 5. WHAT-04 — escala wa_es → wa_br

**Limiares (travados ANTES de rodar contra dados reais, commit `d4fedba2`, ver `git log
--follow -p docs/email-marketing/wa_escalation.py` para a ordem exata):**

| Limiar | Valor | Justificativa |
|---|---|---|
| `MIN_ENVIADOS_ES` | 60 | ordem de grandeza do `MIN_CLICKS=30` do A/B de email (Fase 12), aplicado a canal com resposta muito maior que clique de email |
| `MIN_RESPOSTAS_ES` | 10 | menos que isso é anedota, não taxa |
| `TAXA_MINIMA` | 0.10 (10%) | conservador de propósito; a base histórica pré-cadência (~47%, 8/17) foi descartada por não seguir as travas atuais |
| `TETO_RECUSA` | 0.08 (8%) | mesma lógica do limiar de reversão por bounce do DMARC (D-04, Fase 12) |

**Comando:** `python wa_escalation.py --status` (mede via `wa_estado.py`, imprime veredito, grava
`previews/wa-escalation-state.json`). `python wa_escalation.py --apply` só grava `aplicado=true`
quando o veredito é `AVANCAR`; recusa (exit 1) em qualquer outro caso.

**Veredito atual (medido nesta sessão, histórico completo desde 2026-09-13, quando o audit log de
`outreach-send.py` começou):**

```
Metricas wa_es: {'enviados': 121, 'respondidos': 53, 'recusas': 7, 'taxa': 0.4380165289256198}
Metricas wa_br: {'enviados': 0, 'respondidos': 1, 'recusas': 0, 'taxa': None}
VEREDITO: AVANCAR
motivo: AVANCAR: taxa wa_es 43.8% >= 10.0% minimo, recusa 5.8% <= 8.0% teto (53 respostas em 121 envios)
```

**Achado que muda a leitura do critério de sucesso:** `portao_atual()` (lê o código-fonte real, não
assume) confirma que `export_whatsapp_leads.py` ordena `wa_es` antes de `wa_br`
(`--es-only=True` existe como flag opcional) mas **não exclui** `wa_br`, e
`outreach-send.py:escolhe()` **não filtra por segmento em lugar nenhum** — só por
STOP/blacklist/sent/janela/CAP/prioridade/encoding. Ou seja: **o portão que este requisito pede
para abrir nunca existiu como trava de código no emissor oficial (WHAT-01)**. `wa_br` já pode ser
escolhido pelo emissor assim que a fila de `wa_es` (480 leads, 121 já contatados) se esgotar — por
esgotamento natural, não por decisão. Por isso `--apply`, mesmo com veredito `AVANCAR`, nunca liga
nada: ele grava o veredito e imprime `SEM PORTAO DE SEGMENTO: wa_br ja entra na mesma fila,
ordenada wa_es primeiro — nada a ligar`.

**O que precisa acontecer para o veredito mudar:** mais amostra (a única alavanca honesta — os
limiares não mudam depois de vistos os dados). Se a taxa de resposta cair com o esgotamento da
fila `wa_es`, `--status` vai refletir isso na próxima execução, sem precisar de ação manual.

**Status na rastreabilidade:** `Complete` — mecanismo entregue, provado (`--selftest` 12/12 OK),
rodado sobre dados reais com veredito honesto (`AVANCAR`), e o achado de que não existe portão
formal está documentado e citável (mesmo padrão de equivalência-por-descoberta usado em WHAT-01).

---

## 6. O que o CRM nao sabe (O que o CRM não sabe)

**Divergência medida (não estimada), `previews/whatsapp-estado.json` desta sessão:**

```
enviados_logs: 186   enviados_crm: 17   delta: 169
```

**Causa:** `outreach-send.py` nunca chama `POST /api/v1/integrations/whatsapp-event` — o único
endpoint que o CRM tem para aprender sobre um envio de WhatsApp (`sent`/`reply`/`optout`/`failed`).
O emissor oficial (§2) grava só nos arquivos locais (`logs/outreach-sent.json`,
`logs/outreach-audit.log`); o CRM (`customers.whats_app_sent_count`,
`last_whats_app_sent_at`) nunca é atualizado por ele.

**Efeito prático:**
- O cooldown de 30 dias de `export_whatsapp_leads.py` (`last_whats_app_sent_at`) **não enxerga**
  os 169 contatos que o CRM desconhece — um lead pode, em teoria, ser reexportado e reconsiderado
  como "nunca contatado" pelo lado do CRM, mesmo já tendo recebido mensagem real (a blacklist e o
  `outreach-sent.json` locais são a única proteção real contra reenvio, não o CRM).
- As métricas do CRM (contagem de `whats_app_sent_count`, funil de conversão) **subestimam** o
  canal em ~91% (17 de 186 registrados).
- `whatsapp_weekly_report.py` (§4) resolve isso para o relatório: usa a operação (`wa_estado.py`)
  como fonte primária e expõe o CRM só como conferência — nunca inverte essa prioridade.

**Recomendação explícita (fora do escopo desta fase):** fechar essa lacuna significa adicionar uma
chamada a `POST /api/v1/integrations/whatsapp-event` dentro de `outreach-send.py` logo após
`grava_verificado(SENT, ...)` — mesmo padrão que `wa_event.py` já implementa como CLI standalone.
Fica registrado como candidato de fase futura, não implementado aqui.

---

## 7. Armadilhas conhecidas

- **Incidente 12/09 — dedupe cego no n8n.** `fs.writeFileSync`/`readFileSync` não são confiáveis
  dentro do Code node do n8n e o `catch` vazio engolia o erro — o dedupe ficava cego e o filtro
  sempre pegava o primeiro P1 da base, que por acaso era um lead que já havia recusado em 10/09.
  Causa raiz de `waOutreach1a1AQ` estar desligado até hoje. **Nunca rodar dois emissores (n8n +
  Python) ao mesmo tempo** — o risco de reenvio duplicado é o mesmo, só a causa muda.
- **Mojibake em mensagem (`encoding_quebrado`).** Em 14/09 saiu "Advogado
  Previdenciaoooorio" para um advogado de verdade — causa: um replace global de `'rio'->'ório'`
  rodado mais de uma vez na base corrompeu parte dos nomes. `outreach-send.py:encoding_quebrado()`
  detecta as três marcas conhecidas (`ã³`, `Ã³`, `óó`, `áó`) e **pula** a mensagem em vez de
  tentar consertar às cegas (o próprio conserto às cegas foi o que criou o problema).
- **Saudação congelada.** As 1.618 mensagens da base foram geradas num sábado à noite e
  congelaram "Boa noite"/"Bom sábado" na abertura/fecho. `ajusta_saudacao()` reescreve isso na
  hora do envio, conforme a hora/dia reais.
- **Blacklist vazia = abortar.** `escolhe()` trata blacklist vazia como bug de leitura, nunca
  como "ninguém recusou ainda" — aborta com `SystemExit(4)` em vez de arriscar reenviar para quem
  já recusou.
- **`CLAUDE.md` do repositório está defasado neste ponto e NÃO foi editado aqui** (tem
  modificações não commitadas do usuário, fora de escopo tocar). Ele descreve `"WhatsApp:
  Integrated via Evolution API"` (`WhatsAppController`/`EvolutionApiClient`) — esse é um sistema
  **morto** (`EvolutionApi.ApiKey` vazia em `appsettings.json`, aponta para uma instância
  hospedada não relacionada). O pipeline real desta fase é WAHA local + `outreach-send.py` +
  `IntegrationsController.WhatsAppEvent` — nada deste runbook passa por `WhatsAppController`.

---

## 8. Bateria de verificação

Comandos rodados ao vivo nesta sessão (2026-09-20), saída real reproduzida verbatim:

```
$ python wa_estado.py --status
=== Estado real do canal WhatsApp (2026-09-20T18:40:34) ===
Caminho Python (outreach-send.py, task outreach-wa) — emissor ao vivo hoje:
  enviados hoje: 16/18
  ultimo ENVIADO: 20/09 17:30:13 5527996320214@c.us | Cominoti Construções
  outreach-sent.json (all-time): 186 chat_ids
n8n: porta 5678 FECHADA | task n8n-local: Disabled
  waOutreach1a1AQ: active=0 cap=2 max_prio=4 ultima_execucao=2026-09-12 17:05:36.307
  8nKTlX4Y7Hy1EUEn (bot atendente): active=1 crm_leads=1057 ultima_execucao=2026-09-12 17:14:17.329
Blacklist: 7 numeros
Respostas (7d): 73 {'wa_es': 72, 'wa_br': 1, 'desconhecido': 0}
CRM: {'enviados_total': 17, 'enviados_periodo': 0, 'respostas_task_items': 8, 'respostas_periodo': 0, 'optout': 0}
Divergencia logs-vs-CRM: {'enviados_logs': 186, 'enviados_crm': 17, 'delta': 169, 'causa': 'outreach-send.py nao chama POST /api/v1/integrations/whatsapp-event'}

$ python crm_leads_sync.py --status
fonte=1075 atual=1057 delta=18 pendentes=18 descartados=0

$ python whatsapp_weekly_report.py --dry
📱 <b>Semana de WhatsApp</b>
Enviados: 111 (ES 111 · BR 0)
Respondidos: 53 (ES 52 · BR 1) — taxa 47.7%
  taxa ES: 46.8% · taxa BR: —
Opt-out: 7 (novos: 0)
CRM registra: 0 enviados — divergencia de 111 (outreach-send.py nao escreve no CRM)

$ python wa_escalation.py --selftest
OK 12/12

$ python wa_escalation.py --status
Metricas wa_es: {'enviados': 121, 'respondidos': 53, 'recusas': 7, 'taxa': 0.4380165289256198}
Metricas wa_br: {'enviados': 0, 'respondidos': 1, 'recusas': 0, 'taxa': None}
Portao: {'existe_portao': False, 'onde': 'nenhum', ...}
VEREDITO: AVANCAR
motivo: AVANCAR: taxa wa_es 43.8% >= 10.0% minimo, recusa 5.8% <= 8.0% teto (53 respostas em 121 envios)
```

`schtasks /query` das tasks envolvidas (`MSYS_NO_PATHCONV=1 schtasks /query /tn <nome> /fo LIST /v`
— a variável de ambiente evita o Git Bash reescrever `/query` como caminho):

| Task | Status | Last Result | Last Run Time | Next Run Time |
|---|---|---|---|---|
| `outreach-wa` | Ready | 0 | 9/20/2026 6:00:02 PM | 9/21/2026 9:00:00 AM |
| `outreach-watch` | Running | 267009 (código de "tarefa em execução", não erro) | 9/20/2026 6:40:02 PM | 9/20/2026 7:00:00 PM |
| `DIAX-WA-CrmLeads-Sync` | Ready | 0 | 9/20/2026 6:24:00 PM | 9/20/2026 7:00:00 PM |
| `DIAX-WA-Weekly-Report` | Ready | 0 | 9/20/2026 6:20:30 PM | 9/25/2026 5:00:00 PM (sexta) |

---

## 9. O que nao e versionavel (O que não é versionável)

Nenhuma destas mudanças aparece em `git log` deste repositório (`diax-crm`) — os SUMMARYs de
13-01/02/03/04 são a única rastreabilidade delas:

1. **`D:\claude-code\n8n-local\**`** — workflows (`wa-outreach-1a1.json`,
   `waha-bot-atendente.json`), scripts (`outreach-send.py`, `outreach-watch.py`,
   `restaurar-tudo.py`, `deploy-bot.py`) e logs (`logs/outreach-*.json`,
   `logs/outreach-audit.log`) vivem num repositório **separado** (`n8n-local`), fora do escopo de
   commit deste plano.
2. **`~/.n8n/database.sqlite`** — o `workflow_entity.active`/`staticData` de `waOutreach1a1AQ` e
   `8nKTlX4Y7Hy1EUEn` é estado runtime do n8n, nunca commitado; a única forma de auditar é
   consultar o sqlite ao vivo (como `wa_estado.py`/`crm_leads_sync.py` fazem, sempre
   `mode=ro`) ou reler este runbook.
3. **`D:\claude-code\_scripts\*.vbs`** — `crm-leads-sync.vbs`, `wa-weekly-report.vbs` (wrappers
   sem console para as Scheduled Tasks) ficam fora de qualquer repositório git.
4. **Scheduled Tasks do Windows** (`outreach-wa`, `outreach-watch`, `DIAX-WA-CrmLeads-Sync`,
   `DIAX-WA-Weekly-Report`) — configuração só existe no Task Scheduler do notebook; `schtasks
   /query` (§8) é a única forma de reconferir o estado real.

---

*Phase: 13-escala-de-whatsapp*
*Runbook consolidado gerado em: 2026-09-20 (plano 13-04)*
*Ponteiros: `HANDOFF-whatsapp-leads.md` (documento anterior do canal, parcialmente defasado —
ver §7 sobre a notificação "avisa dono" e o CAP documentado vs real), `ESCALA-EMAIL-runbook.md`
(mesmo formato, canal de email, Phase 12), `QUALIFIED-avanco-automatico.md` (Phase 11),
`OBSERVABILIDADE-watchdogs.md` (Phase 10)*
