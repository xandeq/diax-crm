BEGIN TRANSACTION;
GO

CREATE TABLE [appointments] (
    [id] uniqueidentifier NOT NULL,
    [title] nvarchar(256) NOT NULL,
    [description] nvarchar(256) NULL,
    [date] datetime2 NOT NULL,
    [type] int NOT NULL,
    [daily_notification_sent] bit NOT NULL,
    [user_id] uniqueidentifier NOT NULL,
    [created_at] datetime2 NOT NULL,
    [created_by] nvarchar(256) NULL,
    [updated_at] datetime2 NULL,
    [updated_by] nvarchar(256) NULL,
    CONSTRAINT [PK_appointments] PRIMARY KEY ([id])
);
GO

CREATE INDEX [IX_appointments_user_id] ON [appointments] ([user_id]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260303003214_AddAgendaAppointments', N'8.0.11');
GO

COMMIT;
GO

