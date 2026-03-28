# Runbook de Rotação de Segredos

## Objetivo
Remover dependência operacional de segredos locais/versionados e padronizar rotação controlada.

## Escopo imediato
- JWT signing key
- `Auth:AdminPassword`
- chaves de IA
- SMTP/Brevo
- credenciais de banco
- credenciais Playwright/monitoramento
- API keys administrativas

## Ações executadas no repositório
- `.env` já não é rastreado
- `App_Data/DataProtection-Keys` passou a ser ignorado pelo Git
- suíte de secret scan já existe no GitHub Actions
- exemplos sanitizados de ambiente permanecem em `*.env.example`

## Passo a passo de rotação
1. Inventariar todos os segredos ativos por ambiente.
2. Gerar novos segredos fora do repositório.
3. Atualizar secrets no GitHub Actions e no servidor.
4. Publicar backend e frontend com os novos valores.
5. Revogar imediatamente os segredos antigos.
6. Validar login, integrações, campanhas, IA e financeiro.

## Locais permitidos
- GitHub Actions Secrets
- variáveis de ambiente no servidor
- AWS Secrets Manager quando aplicável

## Locais proibidos
- arquivos versionados
- capturas de tela
- documentos operacionais com valor real
- mensagens em PR ou issues

## Donos
- owner técnico: backend/infra
- owner operacional: deploy/produção

## SLA recomendado
- segredos críticos: rotação em até 24h após suspeita
- segredos padrão: rotação trimestral
- credenciais de serviço terceirizado: rotação semestral
