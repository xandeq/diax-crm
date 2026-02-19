BEGIN TRANSACTION;
GO

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
GO

CREATE INDEX [ix_ai_usage_logs_created_at] ON [ai_usage_logs] ([created_at]);
GO

CREATE INDEX [ix_ai_usage_logs_model_id] ON [ai_usage_logs] ([model_id]);
GO

CREATE INDEX [ix_ai_usage_logs_provider_id] ON [ai_usage_logs] ([provider_id]);
GO

CREATE INDEX [ix_ai_usage_logs_user_created] ON [ai_usage_logs] ([user_id], [created_at]);
GO

CREATE INDEX [ix_ai_usage_logs_user_id] ON [ai_usage_logs] ([user_id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260213014709_AddAiUsageLogs', N'8.0.11');
GO

COMMIT;
GO

