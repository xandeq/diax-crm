# Política de PR e Quality Gate

## Checks obrigatórios para `main`
- `Quality Gate / frontend-build`
- `Quality Gate / backend-test`
- `Security Scan / gitleaks`

## Checks recomendados adicionais
- `Smoke E2E / playwright-smoke`

## Regras
1. PR sem todos os checks obrigatórios aprovados não deve ser mergeado.
2. PR que altera autenticação, financeiro, leads, customers, campanhas ou IA deve ter teste associado.
3. PR com mudança operacional deve atualizar runbook ou documentação correspondente.
4. Segredos nunca devem aparecer em diff, comentário, log ou artifact.

## Estado atual entregue
- build frontend em CI
- build e testes backend em CI
- secret scan em CI
- smoke E2E agendado/manual com Playwright
