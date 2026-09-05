# HANDOFF — WhatsApp 1-a-1 para leads do DIAX CRM (sessão do WAHA/n8n local)

> Para a sessão Claude que opera o WAHA local (`http://127.0.0.1:3000`, sessão `default`,
> número `5527996302718@c.us`, WORKING em 05/09/2026) e o n8n local (`localhost:5678`).
> Contexto completo: `OTIMIZACAO-FUNIL-2026-09-03.md`. Dono: Alexandre Queiroz (AQ).

## 1. O que existe (05/09/2026)
- 7.998 leads no CRM; **1.618 com celular BR válido e sem opt-out** → `previews/whatsapp-leads.json`
  (+ `.csv`), gerado por `python export_whatsapp_leads.py` (só leitura; ~1 min).
- Segmentos: `wa_es` (DDD 27/28) **480** · `wa_br` 1.138. Prioridade:
  P1 respondeu email (2) · P2 clicou/Qualified (1) · P3 wave+follow-up (42) · P4 1 email (752) · P5 nunca (821).
- Cada item: `chat_id` (`55DDD9XXXXXXXX@c.us`), `name`, `niche`, `service`, `finding` (achado real do
  site, quando há) e **`message` pronta** (tom do email, com o achado). Ninguém nunca recebeu WhatsApp.

## 2. Regras (não negociáveis — ban do número + VPS já foi suspensa por "abuso")
1. **1-a-1, humano no loop.** Nada de blast. Começar por P1→P3 (45 leads). P4/P5 só depois de
   medir resposta dos primeiros e com cap.
2. **Cap 20 msgs/dia**, seg-sex 09:00-18:00 BRT, jitter 45-120 s entre envios, máx 1 msg por lead;
   2º toque só se o lead respondeu ou após 7 dias (1 follow-up, depois para).
3. Antes de enviar: `GET /api/contacts/check-exists?phone=<E164 sem +>&session=default` → só se
   `numberExists=true` (evita bater em fixo/inexistente).
4. **Opt-out**: qualquer resposta com "não", "pare", "remover", "sair", "não tenho interesse" →
   `customers.whats_app_opt_out=1` + nunca mais. Resposta positiva → tarefa Urgente no CRM + Telegram.
5. **O bot "WAHA Bot - Atendente IA" (`8nKTlX4Y7Hy1EUEn`) NÃO pode responder lead com IA.**
   Ele recebe todo `message` inbound (`WHATSAPP_HOOK_EVENTS=message`). Filtrar: se `from` ∈ chat_ids
   exportados → **não** responder; encaminhar p/ Telegram + criar tarefa. Lead conversa com o Alexandre.
6. Mensagem: usar a `message` pronta (ou variar 10-20% pra não ficar idêntica). Sem link na 1ª msg.

## 3. Como buscar os leads (3 opções, da mais simples p/ a mais integrada)
**A) Arquivo (recomendado p/ n8n):** `D:\claude-code\diax-crm\docs\email-marketing\previews\whatsapp-leads.json`
   → nó *Read Binary File* + *JSON* → filtrar `priority<=3` (ou `segment=='wa_es'`) → loop com Wait.
   Regenerar: `cd D:\claude-code\diax-crm\docs\email-marketing && python export_whatsapp_leads.py [--es-only] [--limit 40]`.
**B) Banco direto (Python):** `sys.path.insert(0, r'C:\Users\acq20\.claude\skills\financas\scripts'); import db; cx=db.connect()`
   (SQL Server prod `db_aaf0a8_diaxcrm`; creds já no módulo via `.secrets.env`). Colunas úteis:
   `customers.phone, whats_app, whats_app_opt_out, whats_app_sent_count, last_whats_app_sent_at, status, tags`.
**C) API REST:** `POST https://api.alexandrequeiroz.com.br/api/v1/auth/login` (`DIAX_ADMIN_EMAIL/PASSWORD` do
   `.secrets.env`) → `GET /api/v1/leads?search=…&page=1&pageSize=100` (campos `phone`, `whatsApp`, `status`).
   Há também `GET /api/v1/whatsapp/ready-leads` (regra antiga do OutreachService; usa Evolution — **não** WAHA).

## 4. Como enviar (WAHA)
```
POST http://127.0.0.1:3000/api/sendText
X-Api-Key: <WAHA_API_KEY do ~/.claude/.secrets.env>   Content-Type: application/json
{"session":"default","chatId":"5527999990000@c.us","text":"<message>"}
```
Sucesso = resposta com `id` (`fromMe:true`). Marcar "digitando" antes (`POST /api/startTyping` 2-4 s) deixa natural.

## 5. Escrever de volta no CRM (obrigatório — o CRM é quem controla)
**Um único endpoint, sem login, mesma chave do send-email** (`DIAX_SEND_EMAIL_KEY` do `.secrets.env`):
```
POST https://api.alexandrequeiroz.com.br/api/v1/integrations/whatsapp-event
X-Integration-Key: <DIAX_SEND_EMAIL_KEY>      Content-Type: application/json
{"customerId":"<id do JSON>", "event":"sent",   "provider":"waha", "messageId":"<id do WAHA>"}
{"customerId":"<id>",         "event":"reply",  "text":"<o que o lead escreveu>"}
{"phone":"5527999990000",     "event":"optout"}          # phone serve quando não há customerId
{"customerId":"<id>",         "event":"failed", "text":"<erro>"}
```
O que o CRM faz: `sent` → `whats_app_sent_count+1`, `last_whats_app_sent_at`, Lead→Contacted ·
`reply` → status Qualified + **tarefa Urgente** p/ o Alexandre · `optout` → `whats_app_opt_out=1` (sai de
todo export) · `failed` → só log. Resposta 200 `{customerId, event, status, whatsAppSentCount, taskCreated}`;
401 chave errada; 404 lead não achado; 400 evento inválido.
Helper local: `python wa_event.py <customerId|phone> sent|reply|optout|failed ["texto"]`.
No n8n: nó HTTP Request logo após o `sendText` (e no ramo "lead" do inbound com `event=reply`).
Fallback só se a API estiver fora (SQL direto): `UPDATE customers SET whats_app_sent_count=ISNULL(whats_app_sent_count,0)+1,
last_whats_app_sent_at=SYSUTCDATETIME(), updated_by='waha-local' WHERE id='<id>'`.
O `export_whatsapp_leads.py` respeita `whats_app_opt_out` e cooldown de 30 d em `last_whats_app_sent_at`.

## 6. Fluxo no n8n ("WA Outreach 1-a-1") — **JSON pronto p/ importar**: `n8n-wa-outreach-1a1.json`
(mesma pasta; ler `__notas_para_import` dentro do arquivo: credencial `X-Integration-Key`, cred WAHA, teste com CAP=1).
Cron seg-sex 09:05 → ler JSON → filtrar `priority<=3 && !enviado_hoje` → `Limit 20` → loop:
`check-exists` → `startTyping` → `Wait 2-4s` → `sendText` → MSSQL update → `Wait 45-120s` (random) →
fim: Telegram resumo (`n enviados`, `n pulados`). Inbound: no `waha-bot`, primeiro nó *IF from ∈ leads.json*
→ ramo "lead": Telegram + tarefa CRM, **sem** resposta automática.

## 7. Medir
`export_whatsapp_leads.py` + `funnel_analysis.py` (sexta). Meta inicial: 45 leads P1-P3 → ≥5 conversas.
Se resposta < 5% em P3, não abrir P4. Qualquer bloqueio/ban: parar tudo e avisar.
