# Instruções do Projeto DIAX CRM

Você é um especialista em desenvolvimento Full Stack (.NET 8 + Next.js 14) trabalhando no ecossistema DIAX CRM. Use este guia para entender a estrutura do projeto e onde localizar arquivos.

## 1. Estrutura do Workspace (Onde procurar)

O projeto é um monorepo dividido em três responsabilidades principais:

- **Frontend (`crm-web/`)**: Aplicação Next.js (App Router).
- **Backend (`api-core/`)**: API .NET 8 (Clean Architecture).
- **DevOps (`.github/workflows/`)**: Pipelines de CI/CD.

---

## 2. Frontend (`crm-web/`)

**Tecnologias:**
- Next.js 14 (App Router), TypeScript, Tailwind CSS, Lucide React, Shadcn/UI.
- Gerenciamento de estado: Context API (`src/contexts/AuthContext.tsx`) e Hooks.

**Regras de Desenvolvimento:**
- **Localização de Arquivos:**
  - Páginas ficam em `src/app`.
  - Componentes reutilizáveis em `src/components`.
  - Chamadas à API **DEVEM** estar em `src/services`. Não faça fetch direto nos componentes.
- **Integração:**
  - Utilize `src/services/api.ts` para instância base do axios/fetch.
  - O frontend consome a API definida em `NEXT_PUBLIC_API_BASE_URL`.
- **UI:** Use componentes do `src/components/ui` para manter a consistência visual.

---

## 3. Backend (`api-core/`)

**Tecnologias:**
- .NET 8, C#, Entity Framework Core, SQL Server.
- Arquitetura Limpa (Clean Architecture).

**Organização de Camadas:**
- `Diax.Api`: Controllers e configuração de endpoints.
- `Diax.Application`: Regras de negócio, Services (`IApplicationService`), DTOs e Interfaces.
- `Diax.Domain`: Entidades e regras de domínio puras.
- `Diax.Infrastructure`: Implementação de repositórios, contexto do banco (EF Core) e integrações externas.

**Regras de Desenvolvimento:**
- **Endpoints:** Novos endpoints devem ser criados em `Diax.Api/Controllers`.
- **Lógica:** A lógica pesada deve residir em `Diax.Application` (Services), não nos Controllers.
- **Banco de Dados:** Alterações de schema exigem Migrations (`dotnet ef migrations add`).

---

## 3.1. Banco de Dados — REGRAS OBRIGATÓRIAS

> **⚠️ REGRA CRÍTICA: Toda operação de banco de dados (EF Core Migrations, comandos SQL, queries)
> DEVE ser executada contra o banco de PRODUÇÃO (SmarterASP), NUNCA contra o banco local (LocalDB).**

**Banco de Produção (ÚNICO ALVO):**
- **Servidor:** `sql1002.site4now.net` (SmarterASP)
- **Connection String:** Lida automaticamente via `appsettings.Production.json` (arquivo local, não-commitado)
- **Variável de ambiente obrigatória:** `ASPNETCORE_ENVIRONMENT=Production`

**Banco Local (PROIBIDO PARA OPERAÇÕES):**
- ❌ NUNCA executar `dotnet ef database update` sem `ASPNETCORE_ENVIRONMENT=Production`
- ❌ NUNCA executar `sqlcmd` contra `(localdb)\MSSQLLocalDB`
- ❌ NUNCA usar `ConnectionStrings__DefaultConnection` apontando para LocalDB

**Como executar migrations:**
```powershell
cd api-core
.\scripts\update-db.ps1   # Já configurado para PRODUÇÃO
```

**Como executar SQL direto:**
```powershell
# Ler connection string do arquivo local de produção
$cs = (Get-Content "api-core/src/Diax.Api/appsettings.Production.json" | ConvertFrom-Json).ConnectionStrings.DefaultConnection
sqlcmd -S "sql1002.site4now.net" -d "db_aaf0a8_diaxcrm" -U "db_aaf0a8_diaxcrm_admin" -P "SENHA" -Q "SUA QUERY"
```

**Como criar migrations (única exceção — roda local apenas para gerar código):**
```powershell
cd api-core
.\scripts\add-migration.ps1 -Name "NomeDaMigration"
```

---

## 4. Pipelines & DevOps (`.github/workflows/`)

**Objetivo:** Automação de deploy para ambientes de produção.

**Workflows Principais:**
- `deploy-api-core-smarterasp.yml`: Compila a API .NET e faz deploy via FTP para a SmarterASP.
- `deploy-crm-web-hostgator.yml`: Compila o Next.js e faz deploy para Hostgator/Hostinger.

**Regras:**
- Se o usuário pedir para verificar o processo de deploy, analise estes arquivos YAML.
- Os deploys dependem de Secrets do GitHub (`SMARTERASP_FTP_PASSWORD`, etc.).

---

## 5. Funcionalidades Implementadas (Mapa de Contexto)

Use esta seção para entender o que já existe antes de sugerir criar algo novo.

### 5.1. Módulo Financeiro (Completo)
- **Contas & Cartões:** Contas bancárias (`FinancialAccounts`), Cartões de Crédito (`CreditCards`) e Grupos de Cartões.
- **Transações:** Receitas (`Incomes`), Despesas (`Expenses`) e Categorias (`Categories`).
- **Automação:** Importação de Extratos (`StatementImports`) e Transferências entre contas.
- **Reports:** Resumo financeiro e fechamento de faturas.
- **Locais:**
  - Front: `src/app/finance` | `src/services/finance.ts`
  - Back: `Diax.Api/Controllers/V1/Financial*`, `CreditCard*`, `Expense*`, `Income*` | `Diax.Application/Finance`

### 5.2. CRM & Clientes
- **Gestão:** Cadastro de Clientes (`Customers`) e Leads.
- **Locais:**
  - Front: `src/app/customers`, `src/app/leads`
  - Back: `Diax.Api/Controllers/V1/CustomersController.cs`, `LeadsController.cs`

### 5.3. Ferramentas de IA & Utilitários
- **IA Humanize:** Serviço para humanizar textos gerados por IA (`AiHumanizeText`).
- **Html Extraction:** Extração de conteúdo de URLs externas (`HtmlExtraction`).
- **Prompt Generator:** Motor de geração de prompts para LLMs.
- **Snippets:** Gerenciador de trechos de código/texto reutilizáveis.
- **Locais:**
  - Front: `src/app/tools`, `src/app/snippet` | `src/services/htmlExtraction.ts`, `promptGenerator.ts`, `snippetService.ts`
  - Back: `Diax.Api/Controllers/V1/HtmlExtractionController.cs`, `PromptGeneratorController.cs`, `SnippetsController.cs`

### 5.4. Infraestrutura Core
- **Auth:** Login e autenticação JWT.
- **Logs:** Visualização de logs do sistema (`System Logs`).
- **Health:** Endpoint de verificação de saúde da API.

---

## 6. Como responder

- **Contexto:** Se a pergunta for sobre "Tela de Leads", procure em `crm-web/src/app/leads`.
- **Contexto:** Se a pergunta for sobre "Salvar Lead no Banco", procure em `api-core/src/Diax.Application/Customers`.
- **Idioma:** Responda sempre em Português (PT-BR).
