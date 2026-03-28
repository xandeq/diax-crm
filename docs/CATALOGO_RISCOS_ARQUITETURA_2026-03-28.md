# Catálogo de Riscos Arquiteturais

## P0
- Segredos operacionais fora de rotação formal
  - owner: infra/backend
  - prazo: 7 dias
  - ação: executar runbook de rotação e revisar acessos

- Permissões críticas ainda heterogêneas entre controllers
  - owner: backend
  - prazo: 14 dias
  - ação: expandir `RequirePermission` para módulos restantes após mapear grupos e permissões reais

## P1
- Cobertura de testes ainda abaixo da superfície funcional do produto
  - owner: backend/frontend
  - prazo: 21 dias
  - ação: ampliar testes de auth, financeiro, leads, customers e campanhas

- Observabilidade parcial por domínio
  - owner: backend
  - prazo: 14 dias
  - ação: dashboards por módulo, erro e latência

- Smoke E2E depende de credenciais secretas não provisionadas em todos os ambientes
  - owner: devops
  - prazo: 7 dias
  - ação: cadastrar secrets `PLAYWRIGHT_LOGIN_EMAIL` e `PLAYWRIGHT_LOGIN_PASSWORD`

## P2
- Bootstrap da API concentrado em `Program.cs`
  - owner: backend
  - prazo: 30 dias
  - ação: extrair setup por concern

- Falta de política de branch protection documentada
  - owner: engenharia
  - prazo: 7 dias
  - ação: exigir checks `Quality Gate` e `Security Scan` para merge em `main`
