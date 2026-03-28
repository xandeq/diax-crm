# Matriz de Permissões Críticas

## Princípio
- módulos administrativos e sensíveis devem usar permissão explícita
- módulos user-owned devem combinar autenticação + ownership

## Permissões recomendadas

| Módulo | Permissão | Escopo |
|---|---|---|
| usuários | `users.manage` | CRUD de usuários |
| grupos | `groups.manage` | CRUD de grupos e membros |
| permissões | `permissions.manage` | atribuição e revisão RBAC |
| logs | `logs.view` | consulta de logs técnicos |
| auditoria | `audit.view` | consulta de trilha de auditoria |
| api keys | `api-keys.manage` | criação, revogação, visualização |
| ai admin | `ai.manage` | providers, modelos, quotas |
| campanhas | `campaigns.manage` | criação, agendamento, envio |
| campanhas leitura | `campaigns.view` | relatórios e histórico |
| clientes | `customers.manage` | CRUD e operações de CRM |
| leads | `leads.manage` | CRUD, importação, bulk ops |
| financeiro | `finance.manage` | contas, cartões, transações, planner |
| blog | `blog.manage` | posts e publicação |

## Regras por tipo
- `system-admin`
  - acesso total
- `ops-manager`
  - `campaigns.manage`
  - `customers.manage`
  - `leads.manage`
  - `campaigns.view`
- `finance-manager`
  - `finance.manage`
  - `logs.view`
- `support-readonly`
  - `campaigns.view`
  - `logs.view`

## Ações imediatas
- aplicar permissão explícita em todos os controllers administrativos
- mapear controllers user-owned que ainda dependem apenas de `[Authorize]`
- seedar permissões e grupos padrão de forma idempotente
