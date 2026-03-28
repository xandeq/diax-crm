# Plano de Seguranca e Testes

## Seguranca

### Imediato

1. Rotacionar JWT, banco, SMTP, APIs de IA e demais credenciais.
2. Padronizar uso de secrets no GitHub Actions e no ambiente de hospedagem.
3. Proibir novos arquivos de segredo versionados; usar apenas `.env.example`.
4. Ativar varredura automatica de segredos e dependencias.

### Curto prazo

1. Revisar autorizacao por rota e por acao.
2. Revisar CORS, rate limiting e headers de seguranca.
3. Instrumentar auditoria para operacoes financeiras, usuarios, campanhas e API keys.
4. Criar checklist de release com validacao de auth, RBAC e dados sensiveis.

## Testes

### Base minima

1. Unit tests para regras de dominio e seguranca.
2. Integration tests para auth e fluxos financeiros.
3. E2E para login, CRM, campanhas e financeiro.
4. Smoke tests antes de deploy.

### Ordem sugerida

1. Auth
2. Financeiro
3. Leads e clientes
4. Campanhas
5. Automacoes e webhooks
