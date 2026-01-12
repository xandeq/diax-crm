# Configuração do Ambiente

## Frontend (Next.js)

### Variáveis de Ambiente

1. Copie o arquivo de exemplo:
```bash
cd crm-web
cp .env.example .env.local
```

2. Edite `.env.local` e configure a URL da API:
```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5062
```

**Nota**: A porta padrão da API local é `5062`. Ajuste conforme necessário.

3. Reinicie o servidor Next.js para aplicar as mudanças:
```bash
npm run dev
```

### Verificando se a API está correta

Abra o console do navegador (F12) e verifique se as requisições estão sendo feitas para a URL correta. Se você ver erros como "NEXT_PUBLIC_API_BASE_URL is not set", significa que:
- O arquivo `.env.local` não foi criado
- O servidor Next.js não foi reiniciado após criar/editar o arquivo
- A variável está com o nome incorreto

## Backend (.NET 8)

### Build Local

Para verificar se o projeto compila corretamente:

```powershell
cd api-core
dotnet build
```

Se houver erros, corrija-os antes de fazer commit.

### Migrações de Banco de Dados

Criar uma nova migração:
```powershell
cd api-core
dotnet ef migrations add NomeDaMigracao `
  --project src\Diax.Infrastructure `
  --startup-project src\Diax.Api `
  --context DiaxDbContext `
  --output-dir Data\Migrations
```

Aplicar migrações localmente:
```powershell
dotnet ef database update `
  --project src\Diax.Infrastructure `
  --startup-project src\Diax.Api `
  --context DiaxDbContext
```

### Executar API localmente

```powershell
cd api-core/src/Diax.Api
dotnet run
```

A API estará disponível em `http://localhost:5062`.

## Problemas Comuns

### 1. "NEXT_PUBLIC_API_BASE_URL is not set"
- **Solução**: Crie o arquivo `.env.local` conforme instruções acima e reinicie o servidor Next.js

### 2. "Build failed" no GitHub Actions
- **Solução**: Execute `dotnet build` localmente e corrija todos os erros antes de fazer push
- Verifique se todas as migrations estão commitadas
- Certifique-se de que não há erros de compilação (warnings são aceitáveis)

### 3. Erro 404 em rotas do Next.js
- **Solução**: Reinicie o servidor Next.js com `npm run dev`
- Limpe o cache: delete a pasta `.next` e reinicie

### 4. Erro de CORS
- **Solução**: Verifique se `http://localhost:3000` está na lista de origens permitidas no backend
- No arquivo `appsettings.Development.json` deve conter:
  ```json
  {
    "Cors": {
      "AllowedOrigins": ["http://localhost:3000"]
    }
  }
  ```

## Portas Padrão

- **Frontend (Next.js)**: http://localhost:3000
- **Backend (API .NET)**: http://localhost:5062

## Estrutura de Pastas

```
CRM/
├── api-core/              # Backend .NET 8
│   ├── src/
│   │   ├── Diax.Api/      # Endpoints e configuração
│   │   ├── Diax.Application/ # Serviços e DTOs
│   │   ├── Diax.Domain/   # Entidades e interfaces
│   │   ├── Diax.Infrastructure/ # Repositórios e EF Core
│   │   └── Diax.Shared/   # Classes compartilhadas
│   └── tests/
├── crm-web/               # Frontend Next.js 14
│   ├── src/
│   │   ├── app/           # Páginas (App Router)
│   │   ├── components/    # Componentes React
│   │   ├── contexts/      # Contextos (Auth, etc)
│   │   ├── lib/           # Utilitários
│   │   └── services/      # API clients
│   └── public/
└── .github/workflows/     # CI/CD
```

## Deploy

### Backend

O deploy do backend é automático via GitHub Actions quando há push na branch `main` com alterações em `api-core/**`.

**Reset de banco de dados**: Inclua `[reset-db]` na mensagem do commit para forçar a recriação do schema (⚠️ APAGA DADOS).

### Frontend

O deploy do frontend é automático via GitHub Actions quando há push na branch `main` com alterações em `crm-web/**`.

As variáveis de ambiente de produção são configuradas via GitHub Secrets:
- `NEXT_PUBLIC_API_BASE_URL`: URL da API em produção

## Módulo de Logs

O módulo de Logs foi implementado com:

### Backend
- **Domínio**: Entidades `AppLog`, enums `LogLevel` e `LogCategory`
- **Aplicação**: `AppLogService` com DTOs para filtros e paginação
- **Infraestrutura**: Repositório com EF Core, índices para performance
- **API**: 
  - `GET /api/v1/logs` - Lista logs com filtros
  - `GET /api/v1/logs/{id}` - Detalhes de um log
  - `GET /api/v1/logs/stats` - Estatísticas por nível
  - `DELETE /api/v1/logs/cleanup` - Limpeza de logs antigos
- **Middlewares**:
  - `CorrelationIdMiddleware` - Adiciona ID de correlação
  - `ExceptionLoggingMiddleware` - Captura exceções automaticamente

### Frontend
- **Página**: `/logs` (protegida, requer autenticação)
- **Componentes**:
  - `LogFilters` - Filtros expansíveis (data, nível, categoria, etc)
  - `LogsTable` - Tabela com expansão de linhas para detalhes completos
- **Serviço**: `logsService` com integração à API

### Acesso
Após fazer login, o link "Logs" aparece no menu de navegação.
