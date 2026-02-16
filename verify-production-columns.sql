-- Verificar tamanho das colunas phone e whats_app no banco de produção
SELECT
    COLUMN_NAME as 'Coluna',
    DATA_TYPE as 'Tipo',
    CHARACTER_MAXIMUM_LENGTH as 'Tamanho Máximo',
    IS_NULLABLE as 'Nullable'
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'customers'
  AND COLUMN_NAME IN ('phone', 'whats_app', 'email')
ORDER BY COLUMN_NAME;

-- Verificar índices na tabela customers
SELECT
    i.name AS 'Nome do Índice',
    i.is_unique AS 'É Único',
    COL_NAME(ic.object_id, ic.column_id) AS 'Coluna'
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('customers')
  AND COL_NAME(ic.object_id, ic.column_id) IN ('email', 'phone', 'whats_app')
ORDER BY i.name;

-- Verificar migration aplicada
SELECT
    MigrationId as 'Migration ID',
    ProductVersion as 'Versão EF Core'
FROM [__EFMigrationsHistory]
WHERE MigrationId LIKE '%UpdateCustomerPhoneColumnsSize%'
   OR MigrationId LIKE '%AddCustomerImportsTable%';
