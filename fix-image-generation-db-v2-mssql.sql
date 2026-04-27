-- fix-image-generation-db-v2-mssql.sql
-- Migração: Módulo de Geração de Imagens DIAX CRM
-- Convertido para T-SQL (SQL Server) — 2026-04-24
-- DB: sql1002.site4now.net / db_aaf0a8_diaxcrm

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'image_gen_log')
BEGIN
    CREATE TABLE image_gen_log (
        id                INT IDENTITY(1,1) PRIMARY KEY,
        user_id           UNIQUEIDENTIFIER NULL,
        operation         NVARCHAR(20)     NOT NULL DEFAULT 'generate',
        model_key         NVARCHAR(50)     NULL,
        model_id          NVARCHAR(100)    NULL,
        provider          NVARCHAR(20)     NULL,
        prompt            NVARCHAR(MAX)    NULL,
        image_url         NVARCHAR(MAX)    NULL,
        source_image_url  NVARCHAR(MAX)    NULL,
        cost_usd          DECIMAL(10, 6)   NULL DEFAULT 0,
        elapsed_s         DECIMAL(8, 3)    NULL,
        fallback_used     BIT              NOT NULL DEFAULT 0,
        prompt_enhanced   BIT              NOT NULL DEFAULT 0,
        error             NVARCHAR(MAX)    NULL,
        created_at        DATETIMEOFFSET   NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_image_gen_log_users FOREIGN KEY (user_id)
            REFERENCES users(id) ON DELETE SET NULL
    );
    PRINT 'Tabela image_gen_log criada.';
END
ELSE
BEGIN
    PRINT 'Tabela image_gen_log já existe — nenhuma alteração.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_image_gen_log_user'
      AND object_id = OBJECT_ID('image_gen_log'))
BEGIN
    CREATE INDEX idx_image_gen_log_user ON image_gen_log(user_id);
    PRINT 'Index idx_image_gen_log_user criado.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_image_gen_log_created'
      AND object_id = OBJECT_ID('image_gen_log'))
BEGIN
    CREATE INDEX idx_image_gen_log_created ON image_gen_log(created_at DESC);
    PRINT 'Index idx_image_gen_log_created criado.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'idx_image_gen_log_model'
      AND object_id = OBJECT_ID('image_gen_log'))
BEGIN
    CREATE INDEX idx_image_gen_log_model ON image_gen_log(model_key);
    PRINT 'Index idx_image_gen_log_model criado.';
END

EXEC sys.sp_addextendedproperty
    @name       = N'MS_Description',
    @value      = N'Log de geração/edição de imagens — rastreamento de uso, custos e erros por usuário',
    @level0type = N'SCHEMA',
    @level0name = N'dbo',
    @level1type = N'TABLE',
    @level1name = N'image_gen_log';
