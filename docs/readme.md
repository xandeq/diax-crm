\# Documentação – DIAX CRM (Digital Controller)



A pasta \*\*docs\*\* armazena toda a documentação oficial do ecossistema DIAX CRM.  

Aqui ficam:



\- Explicações funcionais do sistema  

\- Documentação técnica para desenvolvedores  

\- Diagramas de arquitetura  

\- Registros de decisões (ADRs)  

\- Guias operacionais  

\- Manuais de integração  

\- Documentação para IA (ChatGPT, Claude, etc.)  

\- Histórico de mudanças importantes  



É o ponto central para entender como o sistema foi projetado, por que certas escolhas foram feitas e como ele funciona de ponta a ponta.



---



\## 🎯 Objetivo da pasta `docs`



Fornecer uma documentação:



\- clara  

\- bem organizada  

\- acessível  

\- explicável para humanos e inteligências artificiais  

\- pronta para crescer junto com o projeto  



---



\## 📂 Estrutura sugerida da pasta



```text

docs/

&nbsp;   system-overview.md           # visão geral do DIAX CRM

&nbsp;   architecture/

&nbsp;       diagrams.md              # diagramas e mapas do sistema

&nbsp;       backend-architecture.md  # arquitetura da API Core

&nbsp;       frontend-architecture.md # arquitetura do CRM Web

&nbsp;       automation-flow.md       # fluxo n8n ponta a ponta

&nbsp;       scraper-flow.md          # fluxo do Google Email Scraper

&nbsp;   adr/                         # Architecture Decision Records

&nbsp;       adr-0001-monorepo.md

&nbsp;       adr-0002-scraper-worker.md

&nbsp;       adr-0003-n8n-orchestration.md

&nbsp;   modules/

&nbsp;       scraper.md               # documentação detalhada do módulo scraper

&nbsp;       crm-web.md               # doc detalhada do CRM

&nbsp;       api-core.md              # doc detalhada da API

&nbsp;       automation.md            # doc detalhada das automações

&nbsp;   operations/

&nbsp;       deploy-guide.md          # guia de deploy completo

&nbsp;       environment-setup.md     # como configurar ambientes

&nbsp;       troubleshooting.md        # problemas comuns

&nbsp;   ai/

&nbsp;       prompts.md               # prompts usados para automações e IA

&nbsp;       patterns.md              # padrões de prompts do sistema

&nbsp;   changelog/

&nbsp;       scraper-changelog.md

&nbsp;       api-core-changelog.md

&nbsp;       crm-web-changelog.md

&nbsp;   README.md                    # este arquivo



