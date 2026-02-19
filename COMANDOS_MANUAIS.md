# 📋 Comandos Manuais - Correção Migração CustomerImports

## Opção 1: Script Automático (RECOMENDADO)

Abra o PowerShell **como Administrador** na raiz do projeto e execute:

```powershell
.\fix-customer-imports-migration.ps1
```

---

## Opção 2: Comandos Manuais (Passo a Passo)

Se preferir executar manualmente, siga os comandos abaixo:

### 1️⃣ Parar API (se estiver rodando)

```powershell
# Localizar processos
Get-Process -Name "Diax.Api"

# Parar processos
Stop-Process -Name "Diax.Api" -Force
```

OU pressione `Ctrl + C` no terminal onde a API está rodando.

---

### 2️⃣ Navegar para o diretório da API

```bash
cd api-core/src/Diax.Api
```

---

### 3️⃣ Remover migração vazia

```bash
dotnet ef migrations remove --project ../Diax.Infrastructure --startup-project . --force
```

**Resultado esperado:**
```
Removing migration '20260215193548_AddCustomerImportsTable'.
Done.
```

---

### 4️⃣ Limpar e reconstruir projeto

```bash
dotnet clean --configuration Debug
dotnet build --configuration Debug --no-incremental
```

**Resultado esperado:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

⚠️ **Se houver erros de build, corrija-os antes de continuar!**

---

### 5️⃣ Criar nova migração

```bash
dotnet ef migrations add AddCustomerImportsTable --project ../Diax.Infrastructure --startup-project .
```

**Resultado esperado:**
```
Build succeeded.
Done. To undo this action, use 'ef migrations remove'
```

**Verificar se a migração foi criada corretamente:**

```bash
# Abrir a migração criada
code ../Diax.Infrastructure/Data/Migrations/[DATA]_AddCustomerImportsTable.cs
```

Deve conter código como:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "customer_imports",
        columns: table => new
        {
            Id = table.Column<Guid>(...),
            FileName = table.Column<string>(...),
            // ... outras colunas
        }
    );
}
```

✅ Se o método `Up()` estiver VAZIO novamente, há um problema mais profundo. Veja seção "Troubleshooting" abaixo.

---

### 6️⃣ Aplicar migração no banco LOCAL

```bash
dotnet ef database update --project ../Diax.Infrastructure --startup-project .
```

**Resultado esperado:**
```
Build succeeded.
Applying migration '20260215XXXXXX_AddCustomerImportsTable'.
Done.
```

---

### 7️⃣ Verificar no banco de dados

**Via SQL Server Management Studio:**
```sql
USE DiaxDb;
GO

-- Verificar se a tabela existe
SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'customer_imports';

-- Verificar estrutura
EXEC sp_help 'customer_imports';

-- Verificar migração aplicada
SELECT * FROM __EFMigrationsHistory
WHERE MigrationId LIKE '%AddCustomerImports%';
```

**Resultado esperado:**
- Tabela `customer_imports` existe
- Registro na `__EFMigrationsHistory`

---

### 8️⃣ Gerar script SQL para PRODUÇÃO

```bash
# Voltar para a raiz do projeto api-core
cd ../..

# Gerar script idempotente
dotnet ef migrations script --idempotent \
  --project src/Diax.Infrastructure \
  --startup-project src/Diax.Api \
  --output ../apply_customer_imports_production.sql
```

**Resultado:** Arquivo `apply_customer_imports_production.sql` criado na raiz do projeto CRM.

---

### 9️⃣ Aplicar em PRODUÇÃO

**⚠️ ATENÇÃO: Faça backup do banco antes!**

**Via SSMS:**
1. Conectar ao servidor de produção
2. Abrir o arquivo `apply_customer_imports_production.sql`
3. Executar (F5)

**Via linha de comando (se tiver acesso):**
```bash
sqlcmd -S SEU_SERVIDOR -d DiaxDb -U usuario -P senha -i apply_customer_imports_production.sql
```

---

### 🔟 Testar a importação

```bash
# Iniciar API local
cd api-core/src/Diax.Api
dotnet run
```

**Acessar no navegador:**
- Frontend: `http://localhost:3000/leads/import`
- API: `http://localhost:5000/swagger`

**Testar importação via Swagger:**
```
POST /api/v1/customers/import
```

**Payload de teste (JSON):**
```json
{
  "contacts": [
    {
      "name": "João Silva",
      "email": "joao@example.com",
      "phone": "11987654321"
    },
    {
      "name": "Maria Santos",
      "email": "maria@example.com",
      "phone": "11987654322"
    }
  ]
}
```

---

## 🔍 Troubleshooting

### Problema: Migração continua vazia

**Possíveis causas:**
1. **Erro de compilação não visível**
2. **DbContext não está carregando a configuração**
3. **Snapshot do modelo corrompido**

**Solução:**

```bash
# 1. Verificar se CustomerImportConfiguration está sendo carregada
cd api-core/src/Diax.Infrastructure

# Abrir DiaxDbContext.cs e verificar linha 144:
# modelBuilder.ApplyConfigurationsFromAssembly(typeof(DiaxDbContext).Assembly);

# 2. Deletar TODAS as migrações relacionadas
cd Data/Migrations
rm *AddCustomerImports*

# 3. Deletar snapshot e recriar
rm DiaxDbContextModelSnapshot.cs

# 4. Voltar e recriar tudo
cd ../../..
cd src/Diax.Api

# 5. Build limpo
dotnet clean
rm -rf bin obj
dotnet build

# 6. Criar migração inicial (se necessário)
dotnet ef migrations add InitialCreate --project ../Diax.Infrastructure --startup-project .

# 7. Criar migração CustomerImports
dotnet ef migrations add AddCustomerImportsTable --project ../Diax.Infrastructure --startup-project .
```

### Problema: Erro "Cannot open database"

**Solução:**
- Verificar se o SQL Server está rodando
- Verificar connection string em `appsettings.json` e `appsettings.Development.json`
- Testar conexão:
  ```bash
  sqlcmd -S localhost -d DiaxDb -E -Q "SELECT @@VERSION"
  ```

### Problema: Erro de permissão

**Solução:**
- Executar PowerShell como Administrador
- Verificar permissões no SQL Server (usuário precisa de `db_ddladmin`)

---

## ✅ Checklist Final

- [ ] API local parada
- [ ] Migração vazia removida
- [ ] Projeto compilando sem erros
- [ ] Nova migração criada (com código no método `Up()`)
- [ ] Migração aplicada no banco LOCAL com sucesso
- [ ] Tabela `customer_imports` existe no banco local
- [ ] Registro em `__EFMigrationsHistory` existe
- [ ] Script SQL de produção gerado
- [ ] Script SQL aplicado em PRODUÇÃO (quando estiver pronto)
- [ ] API iniciada sem erros
- [ ] Importação testada via interface web
- [ ] Logs verificados (sem erros)

---

**Última atualização:** 15/02/2026 21:05
**Status:** Pronto para execução
