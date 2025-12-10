\# API Core – DIAX CRM (Digital Controller)

A \*\*API Core\*\* é o backend central do ecossistema DIAX CRM.  

Ela funciona como a \*\*fonte única de verdade\*\* do sistema, conectando:



\- CRM Web (Next.js)

\- Scraper Google Email (Python)

\- n8n (automação)

\- Banco de dados (MySQL ou SQL Server)

\- Futuras integrações (apps mobile, serviços externos, dashboards)



Tudo passa por ela.



---



\## 🎯 Objetivo Principal



Fornecer uma API segura, consistente e escalável para toda a plataforma DIAX.  

Ela centraliza:



\- Regras de negócio  

\- Persistência de dados  

\- Validações  

\- Autenticação e autorização  

\- Histórico de operações  

\- Log e rastreabilidade  

\- Endpoint de controle de campanhas e scraping  



Este módulo é o \*\*coração\*\* de todo o sistema.



---



\## 🧱 Tecnologias recomendadas



\- \*\*.NET 8 ou 9 (ASP.NET Core Web API)\*\*

\- Entity Framework Core

\- FluentValidation

\- AutoMapper

\- JWT Authentication

\- Swagger (OpenAPI)

\- Arquitetura limpa (Clean Architecture) opcional



---



\## 🗂 Estrutura de Diretórios (sugerida)



```text

api-core/

&nbsp;   src/

&nbsp;       Api/                # Controllers, middlewares, configs

&nbsp;       Application/        # Regras de negócio, DTOs, validadores, serviços

&nbsp;       Domain/             # Entidades e interfaces

&nbsp;       Infrastructure/     # EF Core, repositórios, banco, emails etc.

&nbsp;       Tests/              # Testes unitários e de integração

&nbsp;   configs/

&nbsp;       appsettings.json    # Configurações padrão

&nbsp;       appsettings.\*.json  # Ambientes

&nbsp;   scripts/

&nbsp;       migrate.sh          # Scripts de migração

&nbsp;       seed.sh             # Seed inicial

&nbsp;   README.md



