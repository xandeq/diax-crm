BEGIN TRANSACTION;
GO

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
GO


                INSERT INTO users (id, email, password_hash, is_active, created_at, updated_at)
                SELECT id, email, password_hash, 1, created_at, updated_at
                FROM admin_users
            
GO

ALTER TABLE [user_group_members] DROP CONSTRAINT [FK_user_group_members_admin_users_user_id];
GO

ALTER TABLE [user_group_members] ADD CONSTRAINT [FK_user_group_members_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE CASCADE;
GO

DROP TABLE [admin_users];
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260205211701_RenameAdminUsersToUsers', N'8.0.11');
GO

COMMIT;
GO

