# DIAX CRM - Setup do Ambiente de Desenvolvimento

Este guia explica como configurar o ambiente local para desenvolvimento.

## 📋 Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server 2022](https://www.microsoft.com/sql-server) (local ou remoto)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) ou [VS Code](https://code.visualstudio.com/) + [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

## 🔐 Configuração de Secrets (OBRIGATÓRIO)

### Por que usar User Secrets?

- **Segurança**: Secrets nunca são commitados no repositório
- **Isolamento**: Cada desenvolvedor tem suas próprias credenciais
- **Padrão Microsoft**: Recomendado para desenvolvimento local

### Passo 1: Inicializar User Secrets

Navegue até a pasta do projeto da API e execute:

```bash
cd api-core/src/Diax.Api
dotnet user-secrets init
```

> **Nota**: O projeto já possui `UserSecretsId` configurado no `.csproj`.

### Passo 2: Configurar a Connection String

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=sql1002.site4now.net;Database=db_aaf0a8_diaxcrm;User Id=db_aaf0a8_diaxcrm_admin;Password=SUA_SENHA_AQUI;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

> **Dica**: se sua senha tiver caracteres especiais como `;` ou `"`, prefira o script `scripts/set-local-db-secret.ps1` (ele faz o escaping corretamente).

### Passo 3: Verificar os Secrets

```bash
dotnet user-secrets list
```

Deve mostrar:
```
ConnectionStrings:DefaultConnection = Server=sql1002.site4now.net;...
```

### Onde ficam armazenados?

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\diax-crm-api-secrets\secrets.json`
- **Linux/Mac**: `~/.microsoft/usersecrets/diax-crm-api-secrets/secrets.json`

## 🗄️ Configuração do Entity Framework

### Instalar EF Core Tools (se necessário)

```bash
dotnet tool install --global dotnet-ef
```

Ou atualizar:

```bash
dotnet tool update --global dotnet-ef
```

### Verificar instalação

```bash
dotnet ef --version
```

## 📦 Restaurar Pacotes

Na pasta `api-core`:

```bash
dotnet restore
```

## 🏗️ Migrations

### Criar Nova Migration

Na pasta `api-core`:

```bash
dotnet ef migrations add InitialCustomers --project src/Diax.Infrastructure --startup-project src/Diax.Api --output-dir Data/Migrations
```

### Aplicar Migrations no Banco

```bash
dotnet ef database update --project src/Diax.Infrastructure --startup-project src/Diax.Api
```

### Remover Última Migration (se necessário)

```bash
dotnet ef migrations remove --project src/Diax.Infrastructure --startup-project src/Diax.Api
```

### Gerar Script SQL (para produção manual)

```bash
dotnet ef migrations script --project src/Diax.Infrastructure --startup-project src/Diax.Api --output migrations.sql
```

## 🚀 Executar a API

### Via CLI

```bash
cd api-core
dotnet run --project src/Diax.Api
```

### Via Visual Studio

1. Abra `Diax.CRM.sln`
2. Defina `Diax.Api` como projeto de inicialização
3. Pressione F5

### Endpoints

- **Swagger**: https://localhost:5001/swagger
- **Health Check**: https://localhost:5001/health
- **API Health**: https://localhost:5001/api/v1/health

## 🔧 Variáveis de Ambiente (Produção)

Em produção, configure via variáveis de ambiente:

```bash
# Connection String
DIAX_ConnectionStrings__DefaultConnection="Server=...;Database=...;..."

# JWT Secret
DIAX_Jwt__Secret="sua-chave-secreta-minimo-32-caracteres"
```

> **Nota**: Use `__` (duplo underscore) para representar `:` em variáveis de ambiente.

## 🧪 Executar Testes

```bash
cd api-core
dotnet test
```

## 📝 Comandos Úteis

| Comando | Descrição |
|---------|-----------|
| `dotnet build` | Compilar solução |
| `dotnet run --project src/Diax.Api` | Executar API |
| `dotnet test` | Executar testes |
| `dotnet ef migrations list` | Listar migrations |
| `dotnet user-secrets list` | Listar secrets |
| `dotnet user-secrets clear` | Limpar todos os secrets |


### Banco de Dados (EF Core Migrations)

- Configurar a connection string local via User Secrets (sem commitar segredos):
	- Execute: `powershell -ExecutionPolicy Bypass -File .\scripts\set-local-db-secret.ps1`
- Aplicar migrations no banco configurado:
	- Execute: `powershell -ExecutionPolicy Bypass -File .\scripts\update-db.ps1`
- Criar novas migrations:
	- Execute: `powershell -ExecutionPolicy Bypass -File .\scripts\add-migration.ps1 -Name InitialCreate`

> **Atenção**: se o banco remoto já possui tabelas/migrations antigas (ex.: outra aplicação), as migrations deste projeto podem falhar por conflito de schema. O ideal é usar um banco vazio ou criar um banco novo.

#### Reset do banco remoto (DESTRUTIVO)

Se você confirmar que pode apagar o banco remoto atual, existe um script que remove **todas** as tabelas/objetos e reaplica as migrations:

- Execute: `powershell -ExecutionPolicy Bypass -File .\scripts\reset-remote-db.ps1`

O script exige que você digite `RESET` para confirmar.

## ⚠️ Troubleshooting

### Erro: "Connection string not found"

1. Verifique se User Secrets está configurado: `dotnet user-secrets list`
2. Verifique se está executando em Development: `ASPNETCORE_ENVIRONMENT=Development`

### Erro: está usando a connection string errada (env var sobrescrevendo)

Por padrão, o .NET dá prioridade para variáveis de ambiente sobre User Secrets.

- Se existir `ConnectionStrings__DefaultConnection` no seu Windows com valor placeholder (ex.: `Server=SEU_HOST...`), remova/ajuste.
- O script `scripts/update-db.ps1` ignora automaticamente esse placeholder na sessão.

### Erro: "Login failed for user"

1. Verifique credenciais na connection string
2. Verifique se o banco existe no servidor
3. Verifique se o IP está liberado no firewall do SQL Server

### Erro: "Certificate chain not trusted"

Adicione `TrustServerCertificate=True` na connection string (apenas para desenvolvimento).

## 📚 Referências

- [User Secrets in ASP.NET Core](https://docs.microsoft.com/aspnet/core/security/app-secrets)
- [EF Core Migrations](https://docs.microsoft.com/ef/core/managing-schemas/migrations)
- [SQL Server Connection Strings](https://www.connectionstrings.com/sql-server/)
