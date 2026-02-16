BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216005346_UpdateCustomerPhoneColumnsSize'
)
BEGIN
    DROP INDEX [IX_Customers_Email] ON [customers];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216005346_UpdateCustomerPhoneColumnsSize'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[customers]') AND [c].[name] = N'whats_app');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [customers] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [customers] ALTER COLUMN [whats_app] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216005346_UpdateCustomerPhoneColumnsSize'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[customers]') AND [c].[name] = N'phone');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [customers] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [customers] ALTER COLUMN [phone] nvarchar(50) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216005346_UpdateCustomerPhoneColumnsSize'
)
BEGIN
    CREATE INDEX [IX_Customers_Email] ON [customers] ([email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216005346_UpdateCustomerPhoneColumnsSize'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216005346_UpdateCustomerPhoneColumnsSize', N'8.0.11');
END;
GO

COMMIT;
GO

