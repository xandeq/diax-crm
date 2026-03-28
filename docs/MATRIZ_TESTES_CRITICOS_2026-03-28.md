# Matriz de Testes Críticos

## Smoke

| Fluxo | Cenário | Ferramenta |
|---|---|---|
| login | credenciais válidas redirecionam para dashboard | Playwright |
| financeiro | página personal control abre e carrega resumo | Playwright |
| leads | listagem abre autenticada | Playwright |
| customers | listagem abre autenticada | Playwright |
| campanhas | página de email marketing abre autenticada | Playwright |

## Regressão

| Módulo | Cenários | Ferramenta |
|---|---|---|
| auth | login inválido, login válido, expiração, logout | xUnit + Playwright |
| permissões | deny sem permissão, allow com permissão, admin bypass | xUnit |
| financeiro | criar, editar, excluir, marcar pago, filtros mensais | xUnit + Playwright |
| leads | criar, editar, excluir, bulk delete, import | xUnit + Playwright |
| customers | criar, editar, excluir, timeline, segmentação | xUnit + Playwright |
| campanhas | criar, preview, send-test, queue, paginação | xUnit + Playwright |

## Integração

| Área | Cenários | Ferramenta |
|---|---|---|
| API + DB | auth, permissões, ownership, filtros | xUnit + SQL de teste/Testcontainers |
| webhooks | Brevo/integrações com payload realista | xUnit + mocks HTTP |
| IA | timeout, erro de provider, fallback, quota | xUnit + mocked handlers |
| imports | CSV/PDF e conciliação | xUnit + fixtures |

## Ordem recomendada
1. auth + permissões
2. financeiro
3. leads/customers
4. campanhas
5. imports e integrações externas
