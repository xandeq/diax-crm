BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521181138_AddAiChatTables'
)
BEGIN
    CREATE TABLE [ai_conversations] (
        [id] uniqueidentifier NOT NULL,
        [user_id] uniqueidentifier NOT NULL,
        [title] nvarchar(200) NOT NULL,
        [model] nvarchar(50) NOT NULL,
        [system_prompt] nvarchar(max) NULL,
        [is_archived] bit NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_ai_conversations] PRIMARY KEY ([id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521181138_AddAiChatTables'
)
BEGIN
    CREATE TABLE [ai_chat_messages] (
        [id] uniqueidentifier NOT NULL,
        [conversation_id] uniqueidentifier NOT NULL,
        [role] nvarchar(20) NOT NULL,
        [content] nvarchar(max) NOT NULL,
        [input_tokens] int NOT NULL,
        [output_tokens] int NOT NULL,
        [cache_read_tokens] int NOT NULL,
        [cache_creation_tokens] int NOT NULL,
        [cost_usd] decimal(10,6) NOT NULL,
        [created_at] datetime2 NOT NULL,
        [created_by] nvarchar(256) NULL,
        [updated_at] datetime2 NULL,
        [updated_by] nvarchar(256) NULL,
        CONSTRAINT [PK_ai_chat_messages] PRIMARY KEY ([id]),
        CONSTRAINT [FK_ai_chat_messages_ai_conversations_conversation_id] FOREIGN KEY ([conversation_id]) REFERENCES [ai_conversations] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521181138_AddAiChatTables'
)
BEGIN
    CREATE TABLE [ai_chat_attachments] (
        [id] uniqueidentifier NOT NULL,
        [message_id] uniqueidentifier NOT NULL,
        [file_name] nvarchar(255) NOT NULL,
        [content_type] nvarchar(100) NOT NULL,
        [size_bytes] int NOT NULL,
        [content] nvarchar(max) NOT NULL,
        [created_at] datetime2 NOT NULL,
        CONSTRAINT [PK_ai_chat_attachments] PRIMARY KEY ([id]),
        CONSTRAINT [FK_ai_chat_attachments_ai_chat_messages_message_id] FOREIGN KEY ([message_id]) REFERENCES [ai_chat_messages] ([id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521181138_AddAiChatTables'
)
BEGIN
    CREATE INDEX [IX_ai_chat_attachments_message_id] ON [ai_chat_attachments] ([message_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521181138_AddAiChatTables'
)
BEGIN
    CREATE INDEX [IX_ai_chat_messages_conversation_id_created_at] ON [ai_chat_messages] ([conversation_id], [created_at]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521181138_AddAiChatTables'
)
BEGIN
    CREATE INDEX [IX_ai_conversations_user_id] ON [ai_conversations] ([user_id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521181138_AddAiChatTables'
)
BEGIN
    CREATE INDEX [IX_ai_conversations_user_id_updated_at] ON [ai_conversations] ([user_id], [updated_at] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260521181138_AddAiChatTables'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260521181138_AddAiChatTables', N'8.0.11');
END;
GO

COMMIT;
GO

