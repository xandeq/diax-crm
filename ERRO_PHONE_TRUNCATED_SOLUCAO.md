# 🔧 Solução: Erro Phone Truncated em Produção

## 📋 Resumo do Problema

**Erro reportado:**
```
SqlException: String or binary data would be truncated in table 'customers', column 'phone'.
Truncated value: '+55 27 99981-3701 ::'.
```

**Local**: Importação de clientes via `/api/v1/customers/import`
**Ambiente**: Produção (https://api.alexandrequeiroz.com.br)

### Causa Raiz

A migration `UpdateCustomerPhoneColumnsSize` foi criada localmente mas **NÃO foi aplicada no banco de produção**.

- **Banco local**: Colunas `phone` e `whats_app` = `nvarchar(50)` ✅
- **Banco produção**: Colunas `phone` e `whats_app` = `nvarchar(20)` ❌

Quando um telefone com 26 caracteres (`'+55 27 99981-3701 ::'`) tenta ser inserido, o SQL Server trunca e falha.

---

## ✅ Soluções Disponíveis

### 🚀 Opção 1: PowerShell Script (RECOMENDADO)

**Vantagens:**
- Automático e rápido
- Rastreável (registra na `__EFMigrationsHistory`)
- Gerenciado pelo EF Core

**Passos:**
```powershell
cd "C:\Users\acq20\Desktop\Trabalho\Alexandre Queiroz Marketing Digital\DIAX\CRM"
.\apply-migration-production.ps1
```

O script vai:
1. Solicitar a senha do banco de produção
2. Conectar em `sql1002.site4now.net`
3. Aplicar a migration via `dotnet ef database update`
4. Confirmar sucesso

---

### 📄 Opção 2: Executar SQL Manualmente

**Vantagens:**
- Pode ser revisado antes de executar
- Útil se não tiver acesso via dotnet ef

**Arquivo gerado**: `api-core/update_phone_columns_prod.sql`

**Passos:**
1. Acessar painel SQL do site4now.net
2. Abrir o arquivo `update_phone_columns_prod.sql`
3. Copiar todo o conteúdo
4. Executar no Query Editor

**Instruções detalhadas**: `APLICAR_MIGRATION_PRODUCAO.md`

---

## 🔍 Verificação Pós-Aplicação

### 1. Verificar tamanho das colunas

Executar no banco de produção:
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
| COLUMN_NAME | DATA_TYPE | CHARACTER_MAXIMUM_LENGTH |
|-------------|-----------|--------------------------|
| phone       | nvarchar  | 50                       |
| whats_app   | nvarchar  | 50                       |

### 2. Verificar migration aplicada

```sql
SELECT MigrationId, ProductVersion
FROM [__EFMigrationsHistory]
WHERE MigrationId = '20260216005346_UpdateCustomerPhoneColumnsSize';
```

**Resultado esperado:**
| MigrationId                                    | ProductVersion |
|------------------------------------------------|----------------|
| 20260216005346_UpdateCustomerPhoneColumnsSize  | 8.0.11         |

### 3. Testar importação

1. Acessar: https://crm.alexandrequeiroz.com.br/leads/import
2. Fazer upload do mesmo CSV que estava falhando
3. Confirmar que a importação é bem-sucedida sem erros

---

## 📂 Arquivos Envolvidos

### Criados para solução:
- ✅ `apply-migration-production.ps1` - Script PowerShell para aplicar
- ✅ `api-core/update_phone_columns_prod.sql` - Script SQL idempotente
- ✅ `APLICAR_MIGRATION_PRODUCAO.md` - Instruções detalhadas
- ✅ `ERRO_PHONE_TRUNCATED_SOLUCAO.md` - Este arquivo (resumo)

### Migration:
- `api-core/src/Diax.Infrastructure/Data/Migrations/20260216005346_UpdateCustomerPhoneColumnsSize.cs`

### Código da aplicação (já correto):
- `api-core/src/Diax.Infrastructure/Data/Configurations/CustomerConfiguration.cs` (linhas 44-48)
- `api-core/src/Diax.Application/Customers/CustomerImportService.cs` (função `CleanPhone`)

---

## 🎯 O que a Migration faz

```sql
-- Remove índice único do email
DROP INDEX [IX_Customers_Email] ON [customers];

-- Altera tamanho das colunas
ALTER TABLE [customers] ALTER COLUMN [phone] nvarchar(50) NULL;
ALTER TABLE [customers] ALTER COLUMN [whats_app] nvarchar(50) NULL;

-- Recria índice (não único)
CREATE INDEX [IX_Customers_Email] ON [customers] ([email]);

-- Registra migration
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260216005346_UpdateCustomerPhoneColumnsSize', N'8.0.11');
```

---

## ⚡ Nota Importante

O script SQL é **idempotente** - pode ser executado múltiplas vezes sem causar erros. Ele verifica se a migration já foi aplicada antes de executar qualquer comando.

---

## 📊 Timeline do Problema

1. **15/02/2026 19:00** - Migration criada localmente
2. **15/02/2026 21:44** - Erro reportado em produção ao importar clientes
3. **15/02/2026 23:14** - Script SQL gerado para correção
4. **15/02/2026 23:15** - Scripts PowerShell e documentação criados

---

## 🚀 Próximo Passo

**Execute uma das opções acima** para aplicar a migration no banco de produção e resolver o problema definitivamente.

Após aplicar, o erro não ocorrerá mais e os clientes poderão ser importados normalmente, mesmo com telefones internacionais longos como `+55 27 99981-3701`.
