# 🔧 Guia de Resolução - Sistema de Importação de Contatos

## 🔍 Problema Identificado
**Erro:** `Invalid object name 'customer_imports'`
**Causa:** A migração `AddCustomerImportsTable` não foi aplicada no banco de dados de produção.

## ✅ Solução Passo a Passo

### 1️⃣ Parar a Aplicação Local
A API está rodando e bloqueando o build.

**Opção A - Via Task Manager:**
1. Pressione `Ctrl + Shift + Esc`
2. Procure por processo `Diax.Api` (PID: 12424)
3. Clique com botão direito → "Finalizar Tarefa"

**Opção B - Via PowerShell:**
```powershell
Stop-Process -Name "Diax.Api" -Force
```

### 2️⃣ Gerar Script SQL da Migração

```bash
cd "api-core/src/Diax.Api"
dotnet ef migrations script --idempotent --project ../Diax.Infrastructure --startup-project . --output ../../../customer_imports_migration.sql
```

Este comando:
- Gera um script SQL idempotente (pode rodar várias vezes sem erro)
- Inclui todas as migrações pendentes
- Salva em `customer_imports_migration.sql` na raiz do projeto

### 3️⃣ Revisar o Script SQL

Antes de aplicar, abra o arquivo e verifique:
```sql
-- Deve conter algo como:
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20XXXXXX_AddCustomerImportsTable')
BEGIN
    CREATE TABLE [customer_imports] (
        [Id] uniqueidentifier NOT NULL,
        [file_name] nvarchar(500) NOT NULL,
        [import_type] int NOT NULL,
        [total_rows] int NOT NULL,
        [success_count] int NOT NULL,
        [error_count] int NOT NULL,
        [skipped_count] int NOT NULL,
        [imported_at] datetime2 NOT NULL,
        [imported_by_user_id] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_customer_imports] PRIMARY KEY ([Id])
    );

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20XXXXXX_AddCustomerImportsTable', N'8.0.0');
END
```

### 4️⃣ Aplicar no Banco de Produção

**Via SQL Server Management Studio (SSMS):**
1. Abra SSMS e conecte ao servidor de produção
2. Selecione o banco `DiaxDb` (ou o nome correto)
3. Abra uma nova query
4. Cole o conteúdo de `customer_imports_migration.sql`
5. Execute (F5)

**OU via linha de comando:**
```bash
# Se tiver acesso direto ao servidor
sqlcmd -S seu_servidor -d DiaxDb -i customer_imports_migration.sql
```

**OU via Entity Framework (CUIDADO EM PRODUÇÃO!):**
```bash
cd "api-core/src/Diax.Api"
dotnet ef database update --project ../Diax.Infrastructure --startup-project .
```
⚠️ **ATENÇÃO:** Só use este método se tiver certeza que está apontando para o banco correto!

### 5️⃣ Verificar Migração Aplicada

Execute no SQL Server:
```sql
-- Verificar se a tabela foi criada
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'customer_imports'

-- Verificar registro na tabela de migrações
SELECT * FROM __EFMigrationsHistory WHERE MigrationId LIKE '%AddCustomerImports%'
```

### 6️⃣ Testar a Importação

1. Reinicie a aplicação local
2. Acesse: `http://localhost:5000/leads/import` (ou a porta que estiver usando)
3. Teste com CSV:
```csv
Nome,Email,Telefone
João Silva,joao@example.com,11987654321
Maria Santos,maria@example.com,11987654322
```

4. Ou teste com texto colado:
```
João Silva
joao@example.com
(11) 98765-4321

Maria Santos
maria@example.com
11 98765-4322
```

## 🎯 Checklist de Validação

- [ ] Processo da API local parado
- [ ] Script SQL gerado com sucesso
- [ ] Script revisado e verificado
- [ ] Migração aplicada no banco de produção
- [ ] Tabela `customer_imports` existe no banco
- [ ] Registro inserido em `__EFMigrationsHistory`
- [ ] API reiniciada sem erros
- [ ] Importação testada com CSV
- [ ] Importação testada com texto colado
- [ ] Logs verificados (sem erros de tabela não encontrada)

## 🔄 Próximos Passos Após Resolver

1. **Validar Interface:**
   - Testar parser CSV com diferentes formatos
   - Testar parser de texto com variações de formato
   - Verificar botões de ação (WhatsApp, Email, Converter)

2. **Melhorias Futuras:**
   - [ ] Adicionar botões de ação na página `/customers`
   - [ ] Implementar preview antes da importação
   - [ ] Adicionar validação de telefone brasileiro
   - [ ] Criar dashboard de histórico de importações

## 📝 Notas Importantes

- A migração é **idempotente**: pode rodar várias vezes sem causar erro
- Sempre faça **backup do banco** antes de aplicar migrações em produção
- Em produção, prefira gerar e revisar o SQL manualmente
- O sistema detecta automaticamente se é CSV (com `;` ou `,`) ou texto livre

## 🆘 Em Caso de Erro

**Erro: "Migration already applied"**
- Não há problema, a migração já está aplicada

**Erro: "Cannot connect to database"**
- Verifique a connection string no `appsettings.json`
- Confirme se o SQL Server está rodando

**Erro: "Permission denied"**
- Verifique se o usuário tem permissão para criar tabelas
- Pode precisar de `db_ddladmin` role

---

**Status:** Aguardando aplicação da migração
**Última atualização:** 15/02/2026 20:51
