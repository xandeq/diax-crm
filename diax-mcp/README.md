# DIAX CRM — MCP Server

MCP server que expõe o DIAX CRM para o Claude (Cowork, Claude Code, Claude Desktop).

## Setup

### 1. Instalar dependências e compilar

```bash
cd D:\claude-code\diax-crm\diax-mcp
npm install
npm run build
```

### 2. Garantir credenciais em `~/.claude/.secrets.env`

O MCP lê automaticamente desse arquivo, na ordem env vars → secrets file → defaults.
As chaves usadas:

```env
DIAX_API_URL=https://api.alexandrequeiroz.com.br
DIAX_ADMIN_EMAIL=admin@alexandrequeiroz.com.br
DIAX_ADMIN_PASSWORD=...
# opcional (se configurado no backend):
DIAX_SERVICE_API_KEY=...
```

O MCP **faz login na inicialização** via `POST /api/v1/auth/login` e cacheia o JWT
em memória (válido por 5 anos por padrão). Se o servidor responder 401, faz re-login automaticamente.

Se `DIAX_SERVICE_API_KEY` estiver presente, ele tem prioridade sobre o login JWT.

### 3. Registrar no Claude (global)

Já feito em `C:\Users\acq20\.claude\settings.json`:

```json
"mcpServers": {
  "diax-crm": {
    "command": "node",
    "args": ["D:\\claude-code\\diax-crm\\diax-mcp\\dist\\index.js"]
  }
}
```

Reinicie o Claude para carregar.

---

## Tools disponíveis (13)

| Tool | Descrição |
|---|---|
| `list_leads` | Lista leads com filtros: status, segmento, busca, email, WhatsApp, dias |
| `get_lead` | Dados completos de um lead por ID |
| `create_lead` | Cria novo lead (apenas `name` obrigatório) |
| `update_lead_status` | Avança no funil: Lead→Contacted→Qualified→Negotiating→Customer |
| `update_lead_segment` | Atualiza temperatura: Cold / Warm / Hot |
| `register_contact` | Registra que foi feito contato (atualiza LastContactedAt) |
| `convert_to_customer` | Converte lead em cliente |
| `get_lead_activities` | Timeline de atividades do lead |
| `list_customers` | Lista clientes ativos (status=Customer) |
| `list_tasks` | Lista tarefas — aceita aliases (pending/Todo, completed/Done) |
| `create_task` | Cria nova tarefa |
| `complete_task` | Marca tarefa como Done |
| `financial_summary` | Resumo financeiro por período (padrão: mês corrente) |

---

## Enums

**CustomerStatus:** `0=Lead · 1=Contacted · 2=Qualified · 3=Negotiating · 4=Customer · 5=Inactive · 6=Churned`

**LeadSegment:** `0=Cold · 1=Warm · 2=Hot`

**LeadSource:** `1=Manual · 4=Scraping · 10=Import · 11=GoogleMaps`

**TaskItemStatus** (strings): `Todo · InProgress · Done · Cancelled`

**TaskItemPriority** (strings): `Low · Medium · High · Urgent`

---

## Smoke test manual

```bash
cd D:\claude-code\diax-crm\diax-mcp

# Lista 2 leads quentes pra confirmar conexão
{
  echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"t","version":"1"}}}'
  echo '{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}'
  echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"list_leads","arguments":{"pageSize":2,"segment":2}}}'
  sleep 4
} | node dist/index.js
```

Esperado: stderr `[diax-mcp] Logged in as admin@...` + JSON com `items[]` + `totalCount`.

---

## Dev mode (sem build)

```bash
npm run dev    # usa tsx
```
