# DIAX MCP — Tool Reference

All tools call `api.alexandrequeiroz.com.br` (configurable via `DIAX_API_BASE_URL`).

---

## Leads

### `list_leads`
Lista leads com filtros opcionais. Retorna página com nome, email, status, segmento, fonte e última interação.

| Param | Type | Description |
|---|---|---|
| `search` | string | Busca por nome, email ou empresa |
| `status` | number | `0`=Lead `1`=Contacted `2`=Qualified `3`=Negotiating `4`=Customer `5`=Inactive `6`=Churned |
| `segment` | number | `0`=Cold `1`=Warm `2`=Hot |
| `source` | number | `1`=Manual `4`=Scraping `10`=Import `11`=GoogleMaps |
| `hasEmail` | boolean | Apenas leads com email |
| `hasWhatsApp` | boolean | Apenas leads com WhatsApp |
| `neverEmailed` | boolean | Leads que nunca receberam email |
| `createdAfterDays` | number | Leads criados nos últimos N dias |
| `page` | number | Página (default: 1) |
| `pageSize` | number | Itens por página (default: 20, max: 100) |

### `get_lead`
Dados completos de um lead pelo ID (GUID).

| Param | Required | Description |
|---|---|---|
| `id` | ✅ | GUID do lead |

### `create_lead`
Cria novo lead. Apenas `name` é obrigatório; `email` recomendado para deduplicação.

| Param | Required | Description |
|---|---|---|
| `name` | ✅ | Nome completo |
| `email` | — | Email |
| `phone` | — | Telefone |
| `whatsApp` | — | WhatsApp |
| `companyName` | — | Empresa |
| `website` | — | Site |
| `notes` | — | Observações |
| `tags` | — | Tags separadas por vírgula |
| `source` | — | Fonte (default: 1=Manual) |

### `update_lead_status`
Avança ou recua o lead no funil.

| Param | Required | Description |
|---|---|---|
| `id` | ✅ | GUID |
| `status` | ✅ | `0`–`4` (ver tabela acima) |

### `update_lead_segment`
Atualiza temperatura do lead.

| Param | Required | Description |
|---|---|---|
| `id` | ✅ | GUID |
| `segment` | ✅ | `0`=Cold `1`=Warm `2`=Hot |

### `register_contact`
Registra interação com o lead (atualiza `LastContactedAt` e incrementa contador).

| Param | Required | Description |
|---|---|---|
| `id` | ✅ | GUID |

### `convert_to_customer`
Converte lead em cliente (status → 4=Customer).

| Param | Required | Description |
|---|---|---|
| `id` | ✅ | GUID |

### `get_lead_activities`
Timeline completa de atividades e interações de um lead.

| Param | Required | Description |
|---|---|---|
| `id` | ✅ | GUID |

---

## Customers

### `list_customers`
Lista clientes ativos (status=4). Diferente de `list_leads` que retorna o funil completo.

| Param | Type | Description |
|---|---|---|
| `search` | string | Busca por nome, email ou empresa |
| `page` | number | Página (default: 1) |
| `pageSize` | number | Itens por página (default: 20) |

### `search_customers`
Busca clientes com termo livre e retorna os primeiros N resultados. Atalho para `list_customers` com `search`.

| Param | Required | Description |
|---|---|---|
| `query` | ✅ | Termo de busca |
| `limit` | — | Máx. resultados (default: 10) |

---

## Tasks

### `list_tasks`
Lista tarefas com filtros. Aceita aliases amigáveis para status.

| Param | Type | Description |
|---|---|---|
| `status` | string | `Todo`/`pending` · `InProgress`/`doing` · `Done`/`completed` · `Cancelled` |
| `priority` | string | `Low` · `Medium` · `High` · `Urgent` |
| `overdueOnly` | boolean | Apenas tarefas atrasadas |
| `includeArchived` | boolean | Incluir canceladas |

### `create_task`
Cria tarefa.

| Param | Required | Description |
|---|---|---|
| `title` | ✅ | Título |
| `description` | — | Descrição |
| `dueDate` | — | ISO 8601 (ex: `2026-06-10T10:00:00Z`) |
| `priority` | — | Low/Medium/High/Urgent (default: Medium) |

### `complete_task`
Marca tarefa como Done.

| Param | Required | Description |
|---|---|---|
| `id` | ✅ | GUID da tarefa |

---

## Finance

### `financial_summary`
Resumo financeiro: receitas, despesas, saldo. Sem parâmetros = mês corrente.

| Param | Type | Description |
|---|---|---|
| `startDate` | string | ISO 8601. Default: início do mês |
| `endDate` | string | ISO 8601. Default: fim do mês |

---

## Search & Dashboard

### `search_leads`
Busca leads com termo livre — atalho para `list_leads` com `search`. Retorna os primeiros N resultados com campos essenciais.

| Param | Required | Description |
|---|---|---|
| `query` | ✅ | Termo de busca (nome, email, empresa) |
| `limit` | — | Máx. resultados (default: 10, max: 50) |
| `status` | — | Filtrar por status do funil |

### `dashboard_summary`
Visão geral do CRM: contagem de leads por status, tarefas pendentes, clientes ativos e resumo financeiro do mês. Útil como "ponto de partida" de uma sessão.

Sem parâmetros.

---

## Enum reference

### LeadStatus
| Valor | Nome |
|---|---|
| 0 | Lead |
| 1 | Contacted |
| 2 | Qualified |
| 3 | Negotiating |
| 4 | Customer |
| 5 | Inactive |
| 6 | Churned |

### LeadSegment
| Valor | Nome |
|---|---|
| 0 | Cold |
| 1 | Warm |
| 2 | Hot |

### TaskStatus
| Valor | Nome |
|---|---|
| 1 | Todo |
| 2 | InProgress |
| 3 | Done |
| 4 | Cancelled |

### TaskPriority
| Valor | Nome |
|---|---|
| 1 | Low |
| 2 | Medium |
| 3 | High |
| 4 | Urgent |
