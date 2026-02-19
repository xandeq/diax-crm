IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226100104_InitialCreate'
)
BEGIN
    CREATE TABLE [customers] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [company_name] nvarchar(200) NULL,
        [person_type] int NOT NULL,
        [document] nvarchar(14) NULL,
        [email] nvarchar(255) NOT NULL,
        [secondary_email] nvarchar(255) NULL,
        [phone] nvarchar(20) NULL,
        [whats_app] nvarchar(20) NULL,
        [website] nvarchar(500) NULL,
        [source] int NOT NULL,
        [source_details] nvarchar(500) NULL,
        [notes] nvarchar(4000) NULL,
        [tags] nvarchar(500) NULL,
        [status] int NOT NULL,
        [converted_at] datetime2 NULL,
        [last_contact_at] datetime2 NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(100) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(100) NULL,
        CONSTRAINT [PK_customers] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226100104_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_Customers_Document] ON [customers] ([document]) WHERE [document] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226100104_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Customers_Email] ON [customers] ([email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226100104_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_Name] ON [customers] ([name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226100104_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_Source] ON [customers] ([source]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226100104_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_Status] ON [customers] ([status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226100104_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_Status_CreatedAt] ON [customers] ([status], [created_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251226100104_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251226100104_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228223215_AddAdminUsers'
)
BEGIN
    CREATE TABLE [admin_users] (
        [id] uniqueidentifier NOT NULL,
        [email] nvarchar(255) NOT NULL,
        [password_hash] nvarchar(512) NOT NULL,
        [is_active] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(100) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(100) NULL,
        CONSTRAINT [PK_admin_users] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228223215_AddAdminUsers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AdminUsers_Email] ON [admin_users] ([email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251228223215_AddAdminUsers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251228223215_AddAdminUsers', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101133237_AddFinanceModule'
)
BEGIN
    CREATE TABLE [credit_cards] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(256) NOT NULL,
        [limit] decimal(18,2) NOT NULL,
        [closing_day] int NOT NULL,
        [due_day] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_credit_cards] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101133237_AddFinanceModule'
)
BEGIN
    CREATE TABLE [incomes] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(256) NOT NULL,
        [value] decimal(18,2) NOT NULL,
        [date] datetime2 NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_incomes] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101133237_AddFinanceModule'
)
BEGIN
    CREATE TABLE [expenses] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(256) NOT NULL,
        [value] decimal(18,2) NOT NULL,
        [is_paid] bit NOT NULL,
        [payment_date] datetime2 NULL,
        [payment_method] int NOT NULL,
        [details] nvarchar(256) NULL,
        [due_date] datetime2 NOT NULL,
        [credit_card_id] uniqueidentifier NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_expenses] PRIMARY KEY ([id]),
        CONSTRAINT [FK_expenses_credit_cards_credit_card_id] FOREIGN KEY ([credit_card_id]) REFERENCES [credit_cards] ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101133237_AddFinanceModule'
)
BEGIN
    CREATE INDEX [IX_expenses_credit_card_id] ON [expenses] ([credit_card_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260101133237_AddFinanceModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260101133237_AddFinanceModule', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[expenses]') AND [c].[name] = N'payment_date');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [expenses] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [expenses] DROP COLUMN [payment_date];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    EXEC sp_rename N'[incomes].[value]', N'amount', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    EXEC sp_rename N'[incomes].[name]', N'description', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    EXEC sp_rename N'[expenses].[value]', N'amount', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    EXEC sp_rename N'[expenses].[name]', N'description', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    EXEC sp_rename N'[expenses].[is_paid]', N'is_recurring', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    EXEC sp_rename N'[expenses].[due_date]', N'date', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    EXEC sp_rename N'[expenses].[details]', N'category', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    ALTER TABLE [incomes] ADD [category] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    ALTER TABLE [incomes] ADD [is_recurring] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    ALTER TABLE [incomes] ADD [payment_method] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    ALTER TABLE [credit_cards] ADD [last_four_digits] nvarchar(256) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260104174021_FixIncomeSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260104174021_FixIncomeSchema', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260108100915_AddIncomeCategory'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[incomes]') AND [c].[name] = N'category');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [incomes] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [incomes] DROP COLUMN [category];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260108100915_AddIncomeCategory'
)
BEGIN
    ALTER TABLE [incomes] ADD [income_category_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260108100915_AddIncomeCategory'
)
BEGIN
    CREATE TABLE [income_categories] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(256) NOT NULL,
        [is_active] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_income_categories] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260108100915_AddIncomeCategory'
)
BEGIN
    CREATE INDEX [IX_incomes_income_category_id] ON [incomes] ([income_category_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260108100915_AddIncomeCategory'
)
BEGIN
    ALTER TABLE [incomes] ADD CONSTRAINT [FK_incomes_income_categories_income_category_id] FOREIGN KEY ([income_category_id]) REFERENCES [income_categories] ([id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260108100915_AddIncomeCategory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260108100915_AddIncomeCategory', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260108101003_SeedIncomeCategories'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[income_categories]') AND [c].[name] = N'name');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [income_categories] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [income_categories] ALTER COLUMN [name] nvarchar(100) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260108101003_SeedIncomeCategories'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'created_at', N'created_by', N'is_active', N'name', N'updated_at', N'updated_by') AND [object_id] = OBJECT_ID(N'[income_categories]'))
        SET IDENTITY_INSERT [income_categories] ON;
    EXEC(N'INSERT INTO [income_categories] ([id], [created_at], [created_by], [is_active], [name], [updated_at], [updated_by])
    VALUES (''10000000-0000-0000-0000-000000000001'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Salário'', NULL, NULL),
    (''10000000-0000-0000-0000-000000000002'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Serviço'', NULL, NULL),
    (''10000000-0000-0000-0000-000000000003'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Marketing'', NULL, NULL),
    (''10000000-0000-0000-0000-000000000004'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Venda'', NULL, NULL),
    (''10000000-0000-0000-0000-000000000005'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Comissão'', NULL, NULL),
    (''10000000-0000-0000-0000-000000000006'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Reembolso'', NULL, NULL),
    (''10000000-0000-0000-0000-000000000007'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Empréstimo'', NULL, NULL),
    (''10000000-0000-0000-0000-000000000008'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Outros'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'created_at', N'created_by', N'is_active', N'name', N'updated_at', N'updated_by') AND [object_id] = OBJECT_ID(N'[income_categories]'))
        SET IDENTITY_INSERT [income_categories] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260108101003_SeedIncomeCategories'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260108101003_SeedIncomeCategories', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    ALTER TABLE [incomes] ADD [financial_account_id] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    ALTER TABLE [expenses] ADD [credit_card_invoice_id] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    ALTER TABLE [expenses] ADD [financial_account_id] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    CREATE TABLE [financial_accounts] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [account_type] nvarchar(50) NOT NULL,
        [initial_balance] decimal(18,2) NOT NULL,
        [balance] decimal(18,2) NOT NULL,
        [is_active] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_financial_accounts] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    CREATE TABLE [credit_card_invoices] (
        [id] uniqueidentifier NOT NULL,
        [credit_card_id] uniqueidentifier NOT NULL,
        [reference_month] int NOT NULL,
        [reference_year] int NOT NULL,
        [closing_date] datetime2 NOT NULL,
        [due_date] datetime2 NOT NULL,
        [is_paid] bit NOT NULL,
        [payment_date] datetime2 NULL,
        [paid_from_account_id] uniqueidentifier NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_credit_card_invoices] PRIMARY KEY ([id]),
        CONSTRAINT [FK_credit_card_invoices_credit_cards_credit_card_id] FOREIGN KEY ([credit_card_id]) REFERENCES [credit_cards] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_credit_card_invoices_financial_accounts_paid_from_account_id] FOREIGN KEY ([paid_from_account_id]) REFERENCES [financial_accounts] ([id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    CREATE INDEX [IX_incomes_financial_account_id] ON [incomes] ([financial_account_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    CREATE INDEX [IX_expenses_credit_card_invoice_id] ON [expenses] ([credit_card_invoice_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    CREATE INDEX [IX_expenses_financial_account_id] ON [expenses] ([financial_account_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    CREATE INDEX [IX_credit_card_invoices_paid_from_account_id] ON [credit_card_invoices] ([paid_from_account_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CreditCardInvoices_Card_Period] ON [credit_card_invoices] ([credit_card_id], [reference_month], [reference_year]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    ALTER TABLE [expenses] ADD CONSTRAINT [FK_expenses_credit_card_invoices_credit_card_invoice_id] FOREIGN KEY ([credit_card_invoice_id]) REFERENCES [credit_card_invoices] ([id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    ALTER TABLE [expenses] ADD CONSTRAINT [FK_expenses_financial_accounts_financial_account_id] FOREIGN KEY ([financial_account_id]) REFERENCES [financial_accounts] ([id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    ALTER TABLE [incomes] ADD CONSTRAINT [FK_incomes_financial_accounts_financial_account_id] FOREIGN KEY ([financial_account_id]) REFERENCES [financial_accounts] ([id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109000559_AddFinancialAccountsAndInvoices'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260109000559_AddFinancialAccountsAndInvoices', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    ALTER TABLE [credit_card_invoices] DROP CONSTRAINT [FK_credit_card_invoices_credit_cards_credit_card_id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    EXEC sp_rename N'[credit_card_invoices].[credit_card_id]', N'credit_card_group_id', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    EXEC sp_rename N'[credit_card_invoices].[IX_CreditCardInvoices_Card_Period]', N'IX_CreditCardInvoices_Group_Period', N'INDEX';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    ALTER TABLE [expenses] ADD [paid_date] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    ALTER TABLE [expenses] ADD [status] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    ALTER TABLE [credit_cards] ADD [brand] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    ALTER TABLE [credit_cards] ADD [card_kind] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    ALTER TABLE [credit_cards] ADD [credit_card_group_id] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    ALTER TABLE [credit_cards] ADD [is_active] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    CREATE TABLE [credit_card_groups] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [bank] nvarchar(200) NULL,
        [closing_day] int NOT NULL,
        [due_day] int NOT NULL,
        [shared_limit] decimal(18,2) NOT NULL,
        [is_active] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_credit_card_groups] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    CREATE INDEX [IX_credit_cards_credit_card_group_id] ON [credit_cards] ([credit_card_group_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    CREATE INDEX [IX_CreditCardGroups_IsActive] ON [credit_card_groups] ([is_active]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    CREATE INDEX [IX_CreditCardGroups_Name] ON [credit_card_groups] ([name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    ALTER TABLE [credit_card_invoices] ADD CONSTRAINT [FK_credit_card_invoices_credit_card_groups_credit_card_group_id] FOREIGN KEY ([credit_card_group_id]) REFERENCES [credit_card_groups] ([id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    ALTER TABLE [credit_cards] ADD CONSTRAINT [FK_credit_cards_credit_card_groups_credit_card_group_id] FOREIGN KEY ([credit_card_group_id]) REFERENCES [credit_card_groups] ([id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260109092756_AddCardOrganizationAndExpenseStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260109092756_AddCardOrganizationAndExpenseStatus', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN

                    INSERT INTO financial_accounts (id, name, account_type, initial_balance, balance, is_active, created_at)
                    VALUES (
                        '19631958-7aa0-4856-bdac-301fa32deb78',
                        'Contas Não Atribuídas (Migração)',
                        'Others',
                        0.00,
                        0.00,
                        0,
                        GETDATE()
                    )
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN

                    UPDATE incomes
                    SET financial_account_id = '19631958-7aa0-4856-bdac-301fa32deb78'
                    WHERE financial_account_id IS NULL
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN

                    UPDATE financial_accounts
                    SET balance = (
                        SELECT ISNULL(SUM(amount), 0)
                        FROM incomes
                        WHERE financial_account_id = '19631958-7aa0-4856-bdac-301fa32deb78'
                    )
                    WHERE id = '19631958-7aa0-4856-bdac-301fa32deb78'
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN
    ALTER TABLE [incomes] DROP CONSTRAINT [FK_incomes_financial_accounts_financial_account_id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN
    DROP INDEX [IX_incomes_financial_account_id] ON [incomes];
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[incomes]') AND [c].[name] = N'financial_account_id');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [incomes] DROP CONSTRAINT [' + @var3 + '];');
    EXEC(N'UPDATE [incomes] SET [financial_account_id] = ''00000000-0000-0000-0000-000000000000'' WHERE [financial_account_id] IS NULL');
    ALTER TABLE [incomes] ALTER COLUMN [financial_account_id] uniqueidentifier NOT NULL;
    ALTER TABLE [incomes] ADD DEFAULT '00000000-0000-0000-0000-000000000000' FOR [financial_account_id];
    CREATE INDEX [IX_incomes_financial_account_id] ON [incomes] ([financial_account_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN
    CREATE TABLE [account_transfers] (
        [id] uniqueidentifier NOT NULL,
        [from_financial_account_id] uniqueidentifier NOT NULL,
        [to_financial_account_id] uniqueidentifier NOT NULL,
        [amount] decimal(18,2) NOT NULL,
        [date] datetime2 NOT NULL,
        [description] nvarchar(500) NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_account_transfers] PRIMARY KEY ([id]),
        CONSTRAINT [FK_account_transfers_financial_accounts_from_financial_account_id] FOREIGN KEY ([from_financial_account_id]) REFERENCES [financial_accounts] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_account_transfers_financial_accounts_to_financial_account_id] FOREIGN KEY ([to_financial_account_id]) REFERENCES [financial_accounts] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN
    CREATE INDEX [IX_account_transfers_from_financial_account_id] ON [account_transfers] ([from_financial_account_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN
    CREATE INDEX [IX_account_transfers_to_financial_account_id] ON [account_transfers] ([to_financial_account_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN
    ALTER TABLE [incomes] ADD CONSTRAINT [FK_incomes_financial_accounts_financial_account_id] FOREIGN KEY ([financial_account_id]) REFERENCES [financial_accounts] ([id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110005206_EnforceCashFlowTraceability'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260110005206_EnforceCashFlowTraceability', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    ALTER TABLE [expenses] DROP CONSTRAINT [FK_expenses_credit_cards_credit_card_id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[expenses]') AND [c].[name] = N'description');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [expenses] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [expenses] ALTER COLUMN [description] nvarchar(500) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[expenses]') AND [c].[name] = N'category');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [expenses] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [expenses] ALTER COLUMN [category] nvarchar(200) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    ALTER TABLE [expenses] ADD [expense_category_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    CREATE TABLE [expense_categories] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [is_active] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_expense_categories] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'created_at', N'created_by', N'is_active', N'name', N'updated_at', N'updated_by') AND [object_id] = OBJECT_ID(N'[expense_categories]'))
        SET IDENTITY_INSERT [expense_categories] ON;
    EXEC(N'INSERT INTO [expense_categories] ([id], [created_at], [created_by], [is_active], [name], [updated_at], [updated_by])
    VALUES (''20000000-0000-0000-0000-000000000001'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Alimentação'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000002'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Transporte'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000003'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Moradia'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000004'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Saúde'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000005'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Educação'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000006'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Lazer'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000007'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Vestuário'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000008'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Serviços'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000009'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Impostos'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000010'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Investimentos'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000011'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Marketing'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000012'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Equipamentos'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000013'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Fornecedores'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000014'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Não Categorizado'', NULL, NULL),
    (''20000000-0000-0000-0000-000000000015'', ''2025-01-01T00:00:00.0000000Z'', NULL, CAST(1 AS bit), N''Outros'', NULL, NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'created_at', N'created_by', N'is_active', N'name', N'updated_at', N'updated_by') AND [object_id] = OBJECT_ID(N'[expense_categories]'))
        SET IDENTITY_INSERT [expense_categories] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN

                    -- Update expenses with matching category names
                    UPDATE e
                    SET e.expense_category_id = ec.id
                    FROM expenses e
                    INNER JOIN expense_categories ec ON e.category = ec.name
                    WHERE e.category IS NOT NULL AND e.category != '';

                    -- Set default category for null/empty categories
                    UPDATE expenses
                    SET expense_category_id = '20000000-0000-0000-0000-000000000014'
                    WHERE category IS NULL OR category = '';

                    -- Create categories for any unique category strings not in seed data
                    INSERT INTO expense_categories (id, name, is_active, created_at)
                    SELECT
                        NEWID(),
                        category,
                        1,
                        GETUTCDATE()
                    FROM (
                        SELECT DISTINCT category
                        FROM expenses
                        WHERE category IS NOT NULL
                          AND category != ''
                          AND NOT EXISTS (SELECT 1 FROM expense_categories WHERE name = expenses.category)
                    ) AS unique_categories;

                    -- Map newly created categories
                    UPDATE e
                    SET e.expense_category_id = ec.id
                    FROM expenses e
                    INNER JOIN expense_categories ec ON e.category = ec.name
                    WHERE e.expense_category_id = '00000000-0000-0000-0000-000000000000';
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    CREATE INDEX [IX_expenses_expense_category_id] ON [expenses] ([expense_category_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    ALTER TABLE [expenses] ADD CONSTRAINT [FK_expenses_credit_cards_credit_card_id] FOREIGN KEY ([credit_card_id]) REFERENCES [credit_cards] ([id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    ALTER TABLE [expenses] ADD CONSTRAINT [FK_expenses_expense_categories_expense_category_id] FOREIGN KEY ([expense_category_id]) REFERENCES [expense_categories] ([id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110174927_AddExpenseCategoryAndUpdateExpense'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260110174927_AddExpenseCategoryAndUpdateExpense', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    ALTER TABLE [incomes] DROP CONSTRAINT [FK_incomes_income_categories_income_category_id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[incomes]') AND [c].[name] = N'description');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [incomes] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [incomes] ALTER COLUMN [description] nvarchar(500) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    CREATE TABLE [app_logs] (
        [id] uniqueidentifier NOT NULL,
        [timestamp_utc] datetime2 NOT NULL,
        [level] nvarchar(20) NOT NULL,
        [category] nvarchar(30) NOT NULL,
        [message] nvarchar(4000) NOT NULL,
        [message_template] nvarchar(2000) NULL,
        [source] nvarchar(500) NULL,
        [request_id] nvarchar(100) NULL,
        [correlation_id] nvarchar(100) NULL,
        [user_id] nvarchar(100) NULL,
        [user_name] nvarchar(256) NULL,
        [request_path] nvarchar(2048) NULL,
        [query_string] nvarchar(4000) NULL,
        [http_method] nvarchar(20) NULL,
        [status_code] int NULL,
        [headers_json] nvarchar(max) NULL,
        [client_ip] nvarchar(64) NULL,
        [user_agent] nvarchar(512) NULL,
        [exception_type] nvarchar(500) NULL,
        [exception_message] nvarchar(4000) NULL,
        [stack_trace] nvarchar(max) NULL,
        [inner_exception] nvarchar(max) NULL,
        [target_site] nvarchar(512) NULL,
        [machine_name] nvarchar(128) NULL,
        [environment] nvarchar(64) NULL,
        [additional_data] nvarchar(max) NULL,
        [response_time_ms] bigint NULL,
        CONSTRAINT [PK_app_logs] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    CREATE INDEX [IX_app_logs_correlation_id] ON [app_logs] ([correlation_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    CREATE INDEX [IX_app_logs_level_timestamp] ON [app_logs] ([level], [timestamp_utc]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    CREATE INDEX [IX_app_logs_request_id] ON [app_logs] ([request_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    CREATE INDEX [IX_app_logs_timestamp_utc] ON [app_logs] ([timestamp_utc] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    CREATE INDEX [IX_app_logs_user_id] ON [app_logs] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    ALTER TABLE [incomes] ADD CONSTRAINT [FK_incomes_income_categories_income_category_id] FOREIGN KEY ([income_category_id]) REFERENCES [income_categories] ([id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260112091659_AddAppLogsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260112091659_AddAppLogsTable', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260120120000_AddSnippetsTable'
)
BEGIN
    CREATE TABLE [snippets] (
        [id] uniqueidentifier NOT NULL,
        [title] nvarchar(200) NOT NULL,
        [content] nvarchar(max) NOT NULL,
        [language] nvarchar(50) NOT NULL,
        [is_public] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [expires_at] datetime2 NULL,
        [created_by_user_id] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_snippets] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260120120000_AddSnippetsTable'
)
BEGIN
    CREATE INDEX [IX_snippets_created_by_user_id] ON [snippets] ([created_by_user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260120120000_AddSnippetsTable'
)
BEGIN
    CREATE INDEX [IX_snippets_is_public] ON [snippets] ([is_public]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260120120000_AddSnippetsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260120120000_AddSnippetsTable', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260125153723_AddStatementImportModule'
)
BEGIN
    CREATE TABLE [statement_imports] (
        [id] uniqueidentifier NOT NULL,
        [import_type] int NOT NULL,
        [financial_account_id] uniqueidentifier NULL,
        [credit_card_group_id] uniqueidentifier NULL,
        [file_name] nvarchar(255) NOT NULL,
        [file_content_type] nvarchar(100) NOT NULL,
        [file_size] bigint NOT NULL,
        [status] int NOT NULL,
        [total_records] int NOT NULL,
        [processed_records] int NOT NULL,
        [failed_records] int NOT NULL,
        [error_message] nvarchar(1000) NULL,
        [processed_at] datetime2 NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_statement_imports] PRIMARY KEY ([id]),
        CONSTRAINT [FK_statement_imports_credit_card_groups_credit_card_group_id] FOREIGN KEY ([credit_card_group_id]) REFERENCES [credit_card_groups] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_statement_imports_financial_accounts_financial_account_id] FOREIGN KEY ([financial_account_id]) REFERENCES [financial_accounts] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260125153723_AddStatementImportModule'
)
BEGIN
    CREATE TABLE [imported_transactions] (
        [id] uniqueidentifier NOT NULL,
        [statement_import_id] uniqueidentifier NOT NULL,
        [raw_description] nvarchar(500) NOT NULL,
        [amount] decimal(18,2) NOT NULL,
        [transaction_date] datetime2 NOT NULL,
        [status] int NOT NULL,
        [matched_expense_id] uniqueidentifier NULL,
        [created_expense_id] uniqueidentifier NULL,
        [error_message] nvarchar(1000) NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_imported_transactions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_imported_transactions_expenses_created_expense_id] FOREIGN KEY ([created_expense_id]) REFERENCES [expenses] ([id]),
        CONSTRAINT [FK_imported_transactions_expenses_matched_expense_id] FOREIGN KEY ([matched_expense_id]) REFERENCES [expenses] ([id]),
        CONSTRAINT [FK_imported_transactions_statement_imports_statement_import_id] FOREIGN KEY ([statement_import_id]) REFERENCES [statement_imports] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260125153723_AddStatementImportModule'
)
BEGIN
    CREATE INDEX [IX_imported_transactions_created_expense_id] ON [imported_transactions] ([created_expense_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260125153723_AddStatementImportModule'
)
BEGIN
    CREATE INDEX [IX_imported_transactions_matched_expense_id] ON [imported_transactions] ([matched_expense_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260125153723_AddStatementImportModule'
)
BEGIN
    CREATE INDEX [IX_imported_transactions_statement_import_id] ON [imported_transactions] ([statement_import_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260125153723_AddStatementImportModule'
)
BEGIN
    CREATE INDEX [IX_statement_imports_credit_card_group_id] ON [statement_imports] ([credit_card_group_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260125153723_AddStatementImportModule'
)
BEGIN
    CREATE INDEX [IX_statement_imports_financial_account_id] ON [statement_imports] ([financial_account_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260125153723_AddStatementImportModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260125153723_AddStatementImportModule', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128072838_AddCreatedIncomeIdToImportedTransactions'
)
BEGIN
    ALTER TABLE [imported_transactions] ADD [created_income_id] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128072838_AddCreatedIncomeIdToImportedTransactions'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[expenses]') AND [c].[name] = N'category');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [expenses] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [expenses] ALTER COLUMN [category] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128072838_AddCreatedIncomeIdToImportedTransactions'
)
BEGIN
    CREATE INDEX [IX_imported_transactions_created_income_id] ON [imported_transactions] ([created_income_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128072838_AddCreatedIncomeIdToImportedTransactions'
)
BEGIN
    ALTER TABLE [imported_transactions] ADD CONSTRAINT [FK_imported_transactions_incomes_created_income_id] FOREIGN KEY ([created_income_id]) REFERENCES [incomes] ([id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260128072838_AddCreatedIncomeIdToImportedTransactions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260128072838_AddCreatedIncomeIdToImportedTransactions', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    CREATE TABLE [checklist_categories] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(120) NOT NULL,
        [color] nvarchar(20) NULL,
        [sort_order] int NOT NULL DEFAULT 0,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_checklist_categories] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    CREATE TABLE [checklist_items] (
        [id] uniqueidentifier NOT NULL,
        [category_id] uniqueidentifier NOT NULL,
        [title] nvarchar(160) NOT NULL,
        [description] nvarchar(1000) NULL,
        [status] int NOT NULL,
        [priority] int NULL,
        [target_date] datetime2 NULL,
        [bought_at] datetime2 NULL,
        [canceled_at] datetime2 NULL,
        [estimated_price] decimal(18,2) NULL,
        [actual_price] decimal(18,2) NULL,
        [quantity] decimal(18,2) NULL,
        [store_or_link] nvarchar(500) NULL,
        [is_archived] bit NOT NULL DEFAULT CAST(0 AS bit),
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_checklist_items] PRIMARY KEY ([id]),
        CONSTRAINT [FK_checklist_items_checklist_categories_category_id] FOREIGN KEY ([category_id]) REFERENCES [checklist_categories] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    CREATE INDEX [IX_checklist_categories_name] ON [checklist_categories] ([name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    CREATE INDEX [IX_checklist_categories_sort_order] ON [checklist_categories] ([sort_order]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    CREATE INDEX [IX_checklist_items_category_id] ON [checklist_items] ([category_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    CREATE INDEX [IX_checklist_items_is_archived] ON [checklist_items] ([is_archived]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    CREATE INDEX [IX_checklist_items_status] ON [checklist_items] ([status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    CREATE INDEX [IX_checklist_items_target_date] ON [checklist_items] ([target_date]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    CREATE INDEX [IX_checklist_items_updated_at] ON [checklist_items] ([updated_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260202133347_AddChecklists'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260202133347_AddChecklists', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203103032_AddUserPromptsTable'
)
BEGIN
    CREATE TABLE [user_prompts] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [original_input] nvarchar(4000) NOT NULL,
        [generated_prompt] nvarchar(max) NOT NULL,
        [prompt_type] nvarchar(50) NOT NULL,
        [provider] nvarchar(50) NOT NULL,
        [model] nvarchar(100) NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_user_prompts] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203103032_AddUserPromptsTable'
)
BEGIN
    CREATE INDEX [IX_user_prompts_user_id] ON [user_prompts] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203103032_AddUserPromptsTable'
)
BEGIN
    CREATE INDEX [IX_user_prompts_user_id_created_at] ON [user_prompts] ([user_id], [created_at] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260203103032_AddUserPromptsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260203103032_AddUserPromptsTable', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [statement_imports] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [incomes] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [income_categories] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [imported_transactions] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [financial_accounts] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [expenses] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [expense_categories] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [credit_cards] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [credit_card_invoices] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [credit_card_groups] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    ALTER TABLE [account_transfers] ADD [user_id] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN

                    DECLARE @DefaultUserId UNIQUEIDENTIFIER;
                    SELECT TOP 1 @DefaultUserId = id FROM admin_users ORDER BY created_at;

                    IF @DefaultUserId IS NULL
                        SET @DefaultUserId = '11111111-1111-1111-1111-111111111111';

                    UPDATE statement_imports SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE incomes SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE income_categories SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE imported_transactions SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE financial_accounts SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE expenses SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE expense_categories SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE credit_cards SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE credit_card_invoices SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE credit_card_groups SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                    UPDATE account_transfers SET user_id = @DefaultUserId WHERE user_id = '00000000-0000-0000-0000-000000000000';
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000002'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000003'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000004'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000005'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000006'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000007'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000008'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000009'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000010'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000011'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000012'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000013'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000014'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [expense_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''20000000-0000-0000-0000-000000000015'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [income_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''10000000-0000-0000-0000-000000000001'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [income_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''10000000-0000-0000-0000-000000000002'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [income_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''10000000-0000-0000-0000-000000000003'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [income_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''10000000-0000-0000-0000-000000000004'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [income_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''10000000-0000-0000-0000-000000000005'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [income_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''10000000-0000-0000-0000-000000000006'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [income_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''10000000-0000-0000-0000-000000000007'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    EXEC(N'UPDATE [income_categories] SET [user_id] = ''11111111-1111-1111-1111-111111111111''
    WHERE [id] = ''10000000-0000-0000-0000-000000000008'';
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_statement_imports_user_id] ON [statement_imports] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_incomes_user_id] ON [incomes] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_income_categories_user_id] ON [income_categories] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_imported_transactions_user_id] ON [imported_transactions] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_financial_accounts_user_id] ON [financial_accounts] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_expenses_user_id] ON [expenses] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_expense_categories_user_id] ON [expense_categories] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_credit_cards_user_id] ON [credit_cards] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_credit_card_invoices_user_id] ON [credit_card_invoices] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_credit_card_groups_user_id] ON [credit_card_groups] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    CREATE INDEX [IX_account_transfers_user_id] ON [account_transfers] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205011644_AddUserIdToFinancialEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205011644_AddUserIdToFinancialEntities', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205085713_AddUserRoles'
)
BEGIN
    ALTER TABLE [admin_users] ADD [role] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205085713_AddUserRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205085713_AddUserRoles', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE TABLE [ai_providers] (
        [id] uniqueidentifier NOT NULL,
        [key] nvarchar(50) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [is_enabled] bit NOT NULL,
        [supports_list_models] bit NOT NULL,
        [base_url] nvarchar(255) NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_ai_providers] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE TABLE [permissions] (
        [id] uniqueidentifier NOT NULL,
        [key] nvarchar(100) NOT NULL,
        [description] nvarchar(255) NOT NULL,
        CONSTRAINT [PK_permissions] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE TABLE [user_groups] (
        [id] uniqueidentifier NOT NULL,
        [key] nvarchar(50) NOT NULL,
        [name] nvarchar(100) NOT NULL,
        [is_system] bit NOT NULL,
        [description] nvarchar(500) NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_user_groups] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE TABLE [ai_models] (
        [id] uniqueidentifier NOT NULL,
        [provider_id] uniqueidentifier NOT NULL,
        [model_key] nvarchar(100) NOT NULL,
        [display_name] nvarchar(100) NOT NULL,
        [is_enabled] bit NOT NULL,
        [is_discovered] bit NOT NULL,
        [input_cost_hint] decimal(18,2) NULL,
        [output_cost_hint] decimal(18,2) NULL,
        [max_tokens_hint] int NULL,
        [capabilities_json] nvarchar(256) NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_ai_models] PRIMARY KEY ([id]),
        CONSTRAINT [FK_ai_models_ai_providers_provider_id] FOREIGN KEY ([provider_id]) REFERENCES [ai_providers] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE TABLE [group_ai_provider_access] (
        [group_id] uniqueidentifier NOT NULL,
        [provider_id] uniqueidentifier NOT NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_group_ai_provider_access] PRIMARY KEY ([group_id], [provider_id]),
        CONSTRAINT [FK_group_ai_provider_access_ai_providers_provider_id] FOREIGN KEY ([provider_id]) REFERENCES [ai_providers] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_group_ai_provider_access_user_groups_group_id] FOREIGN KEY ([group_id]) REFERENCES [user_groups] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE TABLE [group_permissions] (
        [group_id] uniqueidentifier NOT NULL,
        [permission_id] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_group_permissions] PRIMARY KEY ([group_id], [permission_id]),
        CONSTRAINT [FK_group_permissions_permissions_permission_id] FOREIGN KEY ([permission_id]) REFERENCES [permissions] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_group_permissions_user_groups_group_id] FOREIGN KEY ([group_id]) REFERENCES [user_groups] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE TABLE [user_group_members] (
        [user_id] uniqueidentifier NOT NULL,
        [group_id] uniqueidentifier NOT NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_user_group_members] PRIMARY KEY ([user_id], [group_id]),
        CONSTRAINT [FK_user_group_members_admin_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [admin_users] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_user_group_members_user_groups_group_id] FOREIGN KEY ([group_id]) REFERENCES [user_groups] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE TABLE [group_ai_model_access] (
        [group_id] uniqueidentifier NOT NULL,
        [ai_model_id] uniqueidentifier NOT NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_group_ai_model_access] PRIMARY KEY ([group_id], [ai_model_id]),
        CONSTRAINT [FK_group_ai_model_access_ai_models_ai_model_id] FOREIGN KEY ([ai_model_id]) REFERENCES [ai_models] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_group_ai_model_access_user_groups_group_id] FOREIGN KEY ([group_id]) REFERENCES [user_groups] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE INDEX [IX_ai_models_provider_id_is_enabled] ON [ai_models] ([provider_id], [is_enabled]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ai_models_provider_id_model_key] ON [ai_models] ([provider_id], [model_key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ai_providers_key] ON [ai_providers] ([key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE INDEX [IX_group_ai_model_access_ai_model_id] ON [group_ai_model_access] ([ai_model_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE INDEX [IX_group_ai_provider_access_provider_id] ON [group_ai_provider_access] ([provider_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE INDEX [IX_group_permissions_permission_id] ON [group_permissions] ([permission_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_permissions_key] ON [permissions] ([key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE INDEX [IX_user_group_members_group_id] ON [user_group_members] ([group_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    CREATE UNIQUE INDEX [IX_user_groups_key] ON [user_groups] ([key]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205133838_AddAiRbacModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205133838_AddAiRbacModule', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205140343_RefineAccessControl'
)
BEGIN
    ALTER TABLE [user_group_members] ADD [user_group_id] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205140343_RefineAccessControl'
)
BEGIN
    CREATE INDEX [IX_user_group_members_user_group_id] ON [user_group_members] ([user_group_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205140343_RefineAccessControl'
)
BEGIN
    ALTER TABLE [user_group_members] ADD CONSTRAINT [FK_user_group_members_user_groups_user_group_id] FOREIGN KEY ([user_group_id]) REFERENCES [user_groups] ([id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205140343_RefineAccessControl'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205140343_RefineAccessControl', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205211701_RenameAdminUsersToUsers'
)
BEGIN
    CREATE TABLE [users] (
        [id] uniqueidentifier NOT NULL,
        [email] nvarchar(100) NOT NULL,
        [password_hash] nvarchar(200) NOT NULL,
        [is_active] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [pk_users] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205211701_RenameAdminUsersToUsers'
)
BEGIN

                    INSERT INTO users (id, email, password_hash, is_active, created_at, updated_at)
                    SELECT id, email, password_hash, 1, created_at, updated_at
                    FROM admin_users
                
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205211701_RenameAdminUsersToUsers'
)
BEGIN
    ALTER TABLE [user_group_members] DROP CONSTRAINT [FK_user_group_members_admin_users_user_id];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205211701_RenameAdminUsersToUsers'
)
BEGIN
    ALTER TABLE [user_group_members] ADD CONSTRAINT [FK_user_group_members_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205211701_RenameAdminUsersToUsers'
)
BEGIN
    DROP TABLE [admin_users];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260205211701_RenameAdminUsersToUsers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260205211701_RenameAdminUsersToUsers', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210220223_AddCreditCardIdToStatementImport'
)
BEGIN
    ALTER TABLE [statement_imports] ADD [credit_card_id] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210220223_AddCreditCardIdToStatementImport'
)
BEGIN
    CREATE INDEX [IX_statement_imports_credit_card_id] ON [statement_imports] ([credit_card_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210220223_AddCreditCardIdToStatementImport'
)
BEGIN
    ALTER TABLE [statement_imports] ADD CONSTRAINT [FK_statement_imports_credit_cards_credit_card_id] FOREIGN KEY ([credit_card_id]) REFERENCES [credit_cards] ([id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260210220223_AddCreditCardIdToStatementImport'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260210220223_AddCreditCardIdToStatementImport', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213010058_AddAiProviderCredentials'
)
BEGIN
    CREATE TABLE [ai_provider_credentials] (
        [id] uniqueidentifier NOT NULL,
        [provider_id] uniqueidentifier NOT NULL,
        [api_key_encrypted] nvarchar(1000) NOT NULL,
        [api_key_last_four_digits] nvarchar(4) NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(100) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(100) NULL,
        CONSTRAINT [PK_ai_provider_credentials] PRIMARY KEY ([id]),
        CONSTRAINT [FK_ai_provider_credentials_ai_providers_provider_id] FOREIGN KEY ([provider_id]) REFERENCES [ai_providers] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213010058_AddAiProviderCredentials'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ai_provider_credentials_provider_id] ON [ai_provider_credentials] ([provider_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213010058_AddAiProviderCredentials'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260213010058_AddAiProviderCredentials', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213014709_AddAiUsageLogs'
)
BEGIN
    CREATE TABLE [ai_usage_logs] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [provider_id] uniqueidentifier NOT NULL,
        [model_id] uniqueidentifier NOT NULL,
        [feature_type] nvarchar(50) NOT NULL,
        [input_tokens] int NULL,
        [output_tokens] int NULL,
        [estimated_cost] decimal(10,6) NULL,
        [duration] time NOT NULL,
        [success] bit NOT NULL,
        [error_message] nvarchar(500) NULL,
        [request_id] nvarchar(100) NOT NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_ai_usage_logs] PRIMARY KEY ([id]),
        CONSTRAINT [FK_ai_usage_logs_ai_models_model_id] FOREIGN KEY ([model_id]) REFERENCES [ai_models] ([id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ai_usage_logs_ai_providers_provider_id] FOREIGN KEY ([provider_id]) REFERENCES [ai_providers] ([id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213014709_AddAiUsageLogs'
)
BEGIN
    CREATE INDEX [ix_ai_usage_logs_created_at] ON [ai_usage_logs] ([created_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213014709_AddAiUsageLogs'
)
BEGIN
    CREATE INDEX [ix_ai_usage_logs_model_id] ON [ai_usage_logs] ([model_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213014709_AddAiUsageLogs'
)
BEGIN
    CREATE INDEX [ix_ai_usage_logs_provider_id] ON [ai_usage_logs] ([provider_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213014709_AddAiUsageLogs'
)
BEGIN
    CREATE INDEX [ix_ai_usage_logs_user_created] ON [ai_usage_logs] ([user_id], [created_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213014709_AddAiUsageLogs'
)
BEGIN
    CREATE INDEX [ix_ai_usage_logs_user_id] ON [ai_usage_logs] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260213014709_AddAiUsageLogs'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260213014709_AddAiUsageLogs', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE TABLE [api_keys] (
        [id] uniqueidentifier NOT NULL,
        [name] nvarchar(200) NOT NULL,
        [key_hash] nvarchar(500) NOT NULL,
        [is_enabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [expires_at] datetime2 NULL,
        [last_used_at] datetime2 NULL,
        [scope] nvarchar(200) NOT NULL DEFAULT N'Blog',
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_api_keys] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE TABLE [blog_posts] (
        [id] uniqueidentifier NOT NULL,
        [title] nvarchar(200) NOT NULL,
        [slug] nvarchar(250) NOT NULL,
        [content_html] nvarchar(256) NOT NULL,
        [excerpt] nvarchar(500) NOT NULL,
        [meta_title] nvarchar(70) NULL,
        [meta_description] nvarchar(160) NULL,
        [keywords] nvarchar(500) NULL,
        [status] int NOT NULL,
        [published_at] datetime2 NULL,
        [author_name] nvarchar(100) NOT NULL,
        [featured_image_url] nvarchar(500) NULL,
        [view_count] int NOT NULL DEFAULT 0,
        [is_featured] bit NOT NULL DEFAULT CAST(0 AS bit),
        [category] nvarchar(100) NULL,
        [tags] nvarchar(500) NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_blog_posts] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE INDEX [IX_api_keys_is_enabled] ON [api_keys] ([is_enabled]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_api_keys_key_hash] ON [api_keys] ([key_hash]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE INDEX [IX_api_keys_name] ON [api_keys] ([name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE INDEX [IX_blog_posts_category] ON [blog_posts] ([category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE INDEX [IX_blog_posts_is_featured] ON [blog_posts] ([is_featured]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE INDEX [IX_blog_posts_published_at] ON [blog_posts] ([published_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE UNIQUE INDEX [IX_blog_posts_slug] ON [blog_posts] ([slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE INDEX [IX_blog_posts_status] ON [blog_posts] ([status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE INDEX [IX_blog_posts_status_published_at] ON [blog_posts] ([status], [published_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    CREATE INDEX [IX_blog_posts_title] ON [blog_posts] ([title]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260214191102_AddApiKeysAndBlogPosts'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260214191102_AddApiKeysAndBlogPosts', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE TABLE [credit_card_strategies] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [credit_card_id] uniqueidentifier NOT NULL,
        [optimal_purchase_day] int NOT NULL,
        [maximum_cycle_length] int NOT NULL,
        [available_limit_percentage] decimal(18,2) NOT NULL,
        [last_calculated] datetime2 NOT NULL,
        [is_recommended] bit NOT NULL,
        [closing_day] int NOT NULL,
        [due_day] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_credit_card_strategies] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE TABLE [financial_goals] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [name] nvarchar(256) NOT NULL,
        [target_amount] decimal(18,2) NOT NULL,
        [current_amount] decimal(18,2) NOT NULL,
        [target_date] datetime2 NULL,
        [category] int NOT NULL,
        [priority] int NOT NULL,
        [is_active] bit NOT NULL,
        [auto_allocate_surplus] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_financial_goals] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE TABLE [monthly_simulations] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [reference_month] int NOT NULL,
        [reference_year] int NOT NULL,
        [simulation_date] datetime2 NOT NULL,
        [starting_balance] decimal(18,2) NOT NULL,
        [projected_ending_balance] decimal(18,2) NOT NULL,
        [total_projected_income] decimal(18,2) NOT NULL,
        [total_projected_expenses] decimal(18,2) NOT NULL,
        [has_negative_balance_risk] bit NOT NULL,
        [first_negative_balance_date] datetime2 NULL,
        [lowest_projected_balance] decimal(18,2) NOT NULL,
        [status] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_monthly_simulations] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE TABLE [recurring_transactions] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [type] int NOT NULL,
        [description] nvarchar(256) NOT NULL,
        [amount] decimal(18,2) NOT NULL,
        [category_id] uniqueidentifier NOT NULL,
        [frequency_type] int NOT NULL,
        [day_of_month] int NOT NULL,
        [start_date] datetime2 NOT NULL,
        [end_date] datetime2 NULL,
        [payment_method] int NOT NULL,
        [credit_card_id] uniqueidentifier NULL,
        [financial_account_id] uniqueidentifier NULL,
        [is_active] bit NOT NULL,
        [priority] int NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_recurring_transactions] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE TABLE [daily_balance_projections] (
        [id] uniqueidentifier NOT NULL,
        [simulation_id] uniqueidentifier NOT NULL,
        [date] datetime2 NOT NULL,
        [opening_balance] decimal(18,2) NOT NULL,
        [total_income] decimal(18,2) NOT NULL,
        [total_expenses] decimal(18,2) NOT NULL,
        [closing_balance] decimal(18,2) NOT NULL,
        [is_negative] bit NOT NULL,
        [has_high_priority_expense] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_daily_balance_projections] PRIMARY KEY ([id]),
        CONSTRAINT [FK_daily_balance_projections_monthly_simulations_simulation_id] FOREIGN KEY ([simulation_id]) REFERENCES [monthly_simulations] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE TABLE [projected_transactions] (
        [id] uniqueidentifier NOT NULL,
        [simulation_id] uniqueidentifier NOT NULL,
        [type] int NOT NULL,
        [description] nvarchar(256) NOT NULL,
        [amount] decimal(18,2) NOT NULL,
        [date] datetime2 NOT NULL,
        [category_id] uniqueidentifier NOT NULL,
        [category_name] nvarchar(256) NOT NULL,
        [payment_method] int NOT NULL,
        [priority] int NOT NULL,
        [source] int NOT NULL,
        [source_id] uniqueidentifier NULL,
        [credit_card_id] uniqueidentifier NULL,
        [financial_account_id] uniqueidentifier NULL,
        [status] int NOT NULL,
        [actual_transaction_id] uniqueidentifier NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_projected_transactions] PRIMARY KEY ([id]),
        CONSTRAINT [FK_projected_transactions_monthly_simulations_simulation_id] FOREIGN KEY ([simulation_id]) REFERENCES [monthly_simulations] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE TABLE [simulation_recommendations] (
        [id] uniqueidentifier NOT NULL,
        [simulation_id] uniqueidentifier NOT NULL,
        [type] int NOT NULL,
        [priority] int NOT NULL,
        [title] nvarchar(256) NOT NULL,
        [message] nvarchar(256) NOT NULL,
        [actionable_transaction_id] uniqueidentifier NULL,
        [suggested_amount] decimal(18,2) NULL,
        [suggested_date] datetime2 NULL,
        [suggested_credit_card_id] uniqueidentifier NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_simulation_recommendations] PRIMARY KEY ([id]),
        CONSTRAINT [FK_simulation_recommendations_monthly_simulations_simulation_id] FOREIGN KEY ([simulation_id]) REFERENCES [monthly_simulations] ([id]) ON DELETE CASCADE,
        CONSTRAINT [FK_simulation_recommendations_projected_transactions_actionable_transaction_id] FOREIGN KEY ([actionable_transaction_id]) REFERENCES [projected_transactions] ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE INDEX [IX_credit_card_strategies_user_id] ON [credit_card_strategies] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE INDEX [IX_daily_balance_projections_simulation_id] ON [daily_balance_projections] ([simulation_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE INDEX [IX_financial_goals_user_id] ON [financial_goals] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE INDEX [IX_monthly_simulations_user_id] ON [monthly_simulations] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE INDEX [IX_projected_transactions_simulation_id] ON [projected_transactions] ([simulation_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE INDEX [IX_recurring_transactions_user_id] ON [recurring_transactions] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE INDEX [IX_simulation_recommendations_actionable_transaction_id] ON [simulation_recommendations] ([actionable_transaction_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    CREATE INDEX [IX_simulation_recommendations_simulation_id] ON [simulation_recommendations] ([simulation_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260215140945_AddFinancialPlannerModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260215140945_AddFinancialPlannerModule', N'8.0.11');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216002310_AddCustomerImportsTable'
)
BEGIN
    CREATE TABLE [customer_imports] (
        [id] uniqueidentifier NOT NULL,
        [file_name] nvarchar(500) NOT NULL,
        [type] int NOT NULL,
        [status] int NOT NULL,
        [total_records] int NOT NULL,
        [success_count] int NOT NULL,
        [failed_count] int NOT NULL,
        [error_details] nvarchar(max) NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(100) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(100) NULL,
        CONSTRAINT [PK_customer_imports] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216002310_AddCustomerImportsTable'
)
BEGIN
    CREATE INDEX [IX_CustomerImports_CreatedAt] ON [customer_imports] ([created_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216002310_AddCustomerImportsTable'
)
BEGIN
    CREATE INDEX [IX_CustomerImports_Status] ON [customer_imports] ([status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260216002310_AddCustomerImportsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260216002310_AddCustomerImportsTable', N'8.0.11');
END;
GO

COMMIT;
GO

