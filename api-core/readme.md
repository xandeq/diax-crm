# DIAX CRM - Core API

**Backend central do ecossistema DIAX CRM** - A fonte única de verdade para todos os dados e operações da empresa.

## 🎯 Visão Geral

Esta API é o núcleo central de dados e operações da DIAX, funcionando como o "sistema nervoso" do negócio. Ela centraliza:

- Cadastro de clientes e leads
- Histórico de relacionamento
- Controle financeiro básico
- Base de e-mails para automações
- Cadastro de projetos e serviços
- Prompts de IA
- Integrações com sistemas externos

## 🏗️ Arquitetura

```
api-core/
├── src/
│   ├── Diax.Api/              # Controllers, Middlewares, Configurações
│   ├── Diax.Application/      # Casos de uso, DTOs, Validators, Services
│   ├── Diax.Domain/           # Entidades, Value Objects, Interfaces
│   ├── Diax.Infrastructure/   # EF Core, Repositórios, Serviços externos
│   └── Diax.Shared/           # Utilitários, Extensions, Result Pattern
├── tests/
│   ├── Diax.UnitTests/        # Testes unitários
│   └── Diax.IntegrationTests/ # Testes de integração
└── Diax.CRM.sln               # Solution principal
```

## 🛠️ Tecnologias

- **.NET 8** - Framework principal
- **ASP.NET Core Web API** - Framework web
- **Entity Framework Core 8** - ORM
- **SQL Server** - Banco de dados
- **FluentValidation** - Validações
- **Serilog** - Logging estruturado
- **Swagger/OpenAPI** - Documentação da API
- **xUnit** - Framework de testes

## 🚀 Como Executar

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local ou remoto)

### Configuração

1. Clone o repositório
2. Configure a connection string em `src/Diax.Api/appsettings.json`
3. Execute as migrations

### Comandos

```bash
# Restaurar pacotes
dotnet restore

# Compilar
dotnet build

# Criar migration (executar na pasta api-core)
dotnet ef migrations add NomeDaMigration --project src/Diax.Infrastructure --startup-project src/Diax.Api --output-dir Data/Migrations

# Aplicar migrations
dotnet ef database update --project src/Diax.Infrastructure --startup-project src/Diax.Api

# Executar API
dotnet run --project src/Diax.Api

# Executar testes
dotnet test
```

## 📡 Endpoints Disponíveis

### Health Check
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/` | Redireciona para Swagger |
| GET | `/health` | Health check básico |
| GET | `/api/v1/health` | Health check com detalhes |

### Customers (Leads/Clientes)
| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/v1/customers` | Lista com paginação e filtros |
| GET | `/api/v1/customers/{id}` | Obtém por ID |
| POST | `/api/v1/customers` | Cria novo customer/lead |
| PUT | `/api/v1/customers/{id}` | Atualiza customer |
| PATCH | `/api/v1/customers/{id}/status` | Atualiza status |
| POST | `/api/v1/customers/{id}/contact` | Registra contato |
| POST | `/api/v1/customers/{id}/convert` | Converte lead → cliente |
| DELETE | `/api/v1/customers/{id}` | Exclui customer |

## 📊 Modelo de Dados: Customer

O modelo unificado de **Customer** representa tanto Leads quanto Clientes:

```
┌─────────────────────────────────────────────────────────────┐
│                        CUSTOMER                              │
├─────────────────────────────────────────────────────────────┤
│ Id (GUID)                    │ PK                           │
│ Name                         │ Nome/Razão Social            │
│ CompanyName                  │ Nome da empresa (opcional)   │
│ PersonType                   │ PF (0) / PJ (1)              │
│ Document                     │ CPF/CNPJ                     │
│ Email                        │ E-mail principal (único)     │
│ SecondaryEmail               │ E-mail secundário            │
│ Phone                        │ Telefone                     │
│ WhatsApp                     │ WhatsApp                     │
│ Website                      │ Site                         │
│ Source                       │ Origem do lead               │
│ SourceDetails                │ Detalhes da origem           │
│ Notes                        │ Observações                  │
│ Tags                         │ Tags (separadas por vírgula) │
│ Status                       │ Status no pipeline           │
│ ConvertedAt                  │ Data de conversão            │
│ LastContactAt                │ Último contato               │
│ CreatedAt / CreatedBy        │ Auditoria criação            │
│ UpdatedAt / UpdatedBy        │ Auditoria atualização        │
└─────────────────────────────────────────────────────────────┘
```

### Pipeline de Status

```
Lead → Contacted → Qualified → Negotiating → Customer → Inactive/Churned
 (0)      (1)         (2)          (3)          (4)        (5)/(6)
```

### Origens (LeadSource)

| Valor | Descrição |
|-------|-----------|
| 0 | Unknown |
| 1 | Manual |
| 2 | Website |
| 3 | Referral (Indicação) |
| 4 | Scraping |
| 5 | Social Media |
| 6 | Email Campaign |
| 7 | Paid Ads |
| 8 | Event |
| 9 | Partnership |
| 10 | Import |

## 📐 Convenções

### Naming
- **Classes/Interfaces**: PascalCase
- **Métodos**: PascalCase
- **Variáveis/Parâmetros**: camelCase
- **Campos privados**: _camelCase

### Estrutura de DTOs
- **Request**: `CreateCustomerRequest`, `UpdateCustomerRequest`
- **Response**: `CustomerResponse`

### Versionamento de API
- URL-based: `/api/v1/`, `/api/v2/`

## 📝 Próximos Passos

1. [x] Implementar domínio de Customers (Leads/Clientes)
2. [ ] Adicionar autenticação JWT
3. [ ] Implementar domínio de Interações/Histórico
4. [ ] Implementar domínio Financeiro
5. [ ] Adicionar cache com Redis
6. [ ] Configurar CI/CD

## 📄 Licença

Projeto proprietário - DIAX © 2024
