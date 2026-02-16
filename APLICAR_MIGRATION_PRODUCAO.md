# 🔧 Aplicar Migration no Banco de Produção

## ⚠️ Problema Identificado

Erro ao importar clientes na produção:
```
SqlException: String or binary data would be truncated in table 'customers', column 'phone'.
Truncated value: '+55 27 99981-3701 ::'.
```

**Causa**: A migration `UpdateCustomerPhoneColumnsSize` foi criada localmente mas **NÃO foi aplicada no banco de produção**.

O banco de produção ainda tem as colunas `phone` e `whats_app` com tamanho `nvarchar(20)`, mas o código da aplicação está tentando inserir valores com até 50 caracteres.

---

## ✅ Solução: Executar Script SQL

Um script SQL idempotente foi gerado em:
```
api-core/update_phone_columns_prod.sql
```

### Passos para aplicar:

1. **Acessar o painel de administração do banco** (site4now.net ou SQL Server Management Studio)
   - Server: `sql1002.site4now.net` (ou `SQL8020.site4now.net`)
   - Database: `db_aaf0a8_diaxcrm`
   - User: `db_aaf0a8_diaxcrm_admin`

2. **Executar o script SQL**:
   - Abrir o arquivo `api-core/update_phone_columns_prod.sql`
   - Copiar todo o conteúdo
   - Executar no Query Editor do painel de administração

3. **O script fará**:
   - ✅ Verificar se a migration já foi aplicada (idempotente)
   - ✅ Remover índice único do email (já foi feito antes)
   - ✅ Alterar coluna `phone` de nvarchar(20) → nvarchar(50)
   - ✅ Alterar coluna `whats_app` de nvarchar(20) → nvarchar(50)
   - ✅ Recriar índice não-único no email
   - ✅ Registrar a migration na tabela `__EFMigrationsHistory`

---

## 🔍 Verificação Pós-Aplicação

Após executar o script, executar as seguintes queries para verificar:

### 1. Verificar tamanho das colunas:
```sql
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'customers'
  AND COLUMN_NAME IN ('phone', 'whats_app');
```

**Resultado esperado:**
```
COLUMN_NAME    DATA_TYPE    CHARACTER_MAXIMUM_LENGTH
phone          nvarchar     50
whats_app      nvarchar     50
```

### 2. Verificar migration aplicada:
```sql
SELECT MigrationId, ProductVersion
FROM [__EFMigrationsHistory]
WHERE MigrationId = '20260216005346_UpdateCustomerPhoneColumnsSize';
```

**Resultado esperado:**
```
MigrationId                                    ProductVersion
20260216005346_UpdateCustomerPhoneColumnsSize  8.0.11
```

### 3. Testar importação:
- Acessar https://crm.alexandrequeiroz.com.br/leads/import
- Tentar importar o mesmo arquivo CSV que estava falhando
- Verificar se a importação é bem-sucedida

---

## 📋 Conteúdo do Script

O script completo está em `api-core/update_phone_columns_prod.sql`.

Resumo:
```sql
BEGIN TRANSACTION;

-- Verifica se migration já foi aplicada
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260216005346_UpdateCustomerPhoneColumnsSize')
BEGIN
    -- Remove índice único do email
    DROP INDEX [IX_Customers_Email] ON [customers];

    -- Altera tamanho da coluna whats_app
    ALTER TABLE [customers] ALTER COLUMN [whats_app] nvarchar(50) NULL;

    -- Altera tamanho da coluna phone
    ALTER TABLE [customers] ALTER COLUMN [phone] nvarchar(50) NULL;

    -- Recria índice não-único
    CREATE INDEX [IX_Customers_Email] ON [customers] ([email]);

    -- Registra migration
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216005346_UpdateCustomerPhoneColumnsSize', N'8.0.11');
END;

COMMIT;
```

---

## ⚡ Nota Importante

Este script é **idempotente** - pode ser executado múltiplas vezes sem causar erros. Se a migration já tiver sido aplicada, o script não fará nada.

---

## 🆘 Em caso de erro

Se houver algum erro durante a execução:

1. **Erro "Index already exists"**: Ignorar, significa que o índice já foi criado
2. **Erro "Cannot drop index"**: Verificar se existe índice `IX_Customers_Email` único e removê-lo manualmente
3. **Erro de permissão**: Verificar se o usuário `db_aaf0a8_diaxcrm_admin` tem permissões de ALTER TABLE

Para assistência, verificar logs completos na mensagem de erro do SQL Server.
