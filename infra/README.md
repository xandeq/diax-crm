\# Infraestrutura – DIAX CRM (Digital Controller)



A pasta \*\*infra\*\* contém toda a infraestrutura necessária para instalar, executar, escalar e manter o ecossistema DIAX CRM.  

Ela inclui:



\- Pipelines de CI/CD

\- Scripts de deploy

\- Arquivos Docker

\- Configurações de ambiente (`.env`)

\- Provisionamento de VPS

\- Setup de serviços externos

\- Padronização de ambientes (dev, staging, prod)



Aqui é onde definimos \*\*como o sistema roda\*\*, não apenas \*\*o que o sistema faz\*\*.



---



\## 🎯 Objetivo



Manter a operação do DIAX CRM:



\- previsível  

\- automatizada  

\- reprodutível  

\- fácil de restaurar  

\- fácil de documentar  

\- fácil de escalar  



Ou seja: organizar a parte invisível que faz o sistema funcionar no mundo real.



---



\## 🧱 Estrutura recomendada da pasta



```text

infra/

&nbsp;   deploy/

&nbsp;       scraper/            # Deploy do scraper Python na VPS

&nbsp;       api-core/           # Deploy da API .NET na VPS/SmarterASP

&nbsp;       crm-web/            # Deploy do Next.js (Hostinger/Node)

&nbsp;   docker/

&nbsp;       scraper/            # Dockerfile do scraper

&nbsp;       api-core/           # Dockerfile opcional da API

&nbsp;       crm-web/            # Dockerfile do CRM

&nbsp;   ci-cd/

&nbsp;       github-actions/     # Workflows GitHub Actions

&nbsp;       azure-devops/       # Pipelines YAML (opcional)

&nbsp;   env/

&nbsp;       .env.example        # Variáveis padrão do projeto

&nbsp;       scraper.env         # Configuração específica do scraper

&nbsp;       api.env             # Configuração da API

&nbsp;       crm.env             # Configuração do Next.js

&nbsp;   scripts/

&nbsp;       setup-vps.sh        # Configurar VPS Hostinger

&nbsp;       install-deps.sh     # Instalar dependências comuns

&nbsp;       restart-services.sh # Reiniciar serviços do controlador digital

&nbsp;   README.md



