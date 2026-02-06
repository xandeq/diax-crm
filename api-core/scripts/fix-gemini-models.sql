-- Script de Atualização do Catálogo de Modelos IA (Gemini 2.5/2.0 + Fixes)
-- Alvo: Banco de Produção (SmarterASP)
-- Data: 2026-02-06

DECLARE @GeminiProviderId UNIQUEIDENTIFIER;

-- 1. Localizar o ID do provedor 'gemini'
SELECT @GeminiProviderId = Id
FROM AiProviders
WHERE [Key] = 'gemini';

IF @GeminiProviderId IS NOT NULL
BEGIN
    PRINT 'Provedor Gemini encontrado. Iniciando atualização de modelos...';

    -- Lista de modelos para garantir que existam
    -- FORMATO: ModelKey, DisplayName
    DECLARE @ModelsToUpdate TABLE (ModelKey NVARCHAR(100), DisplayName NVARCHAR(200));

    INSERT INTO @ModelsToUpdate (ModelKey, DisplayName)
    VALUES
        ('gemini-2.5-flash', 'Gemini 2.5 Flash'),
        ('gemini-2.0-flash', 'Gemini 2.0 Flash'),
        ('gemini-1.5-flash', 'Gemini 1.5 Flash'),
        ('gemma-3-4b-it', 'Gemma 3 4B IT'),
        ('gemma-3-12b-it', 'Gemma 3 12B IT');

    -- Cursor ou Loop para inserir os modelos faltantes
    DECLARE @mKey NVARCHAR(100), @mName NVARCHAR(200);

    DECLARE cur CURSOR FOR SELECT ModelKey, DisplayName FROM @ModelsToUpdate;
    OPEN cur;
    FETCH NEXT FROM cur INTO @mKey, @mName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM AiModels WHERE ProviderId = @GeminiProviderId AND ModelKey = @mKey)
        BEGIN
            INSERT INTO AiModels (Id, ProviderId, ModelKey, DisplayName, IsEnabled, CreatedAt, IsDiscovered)
            VALUES (NEWID(), @GeminiProviderId, @mKey, @mName, 1, GETUTCDATE(), 0);
            PRINT 'SUCCESS: Modelo ' + @mKey + ' inserido e habilitado.';
        END
        ELSE
        BEGIN
            -- Se já existe, garante que está habilitado
            UPDATE AiModels SET IsEnabled = 1 WHERE ProviderId = @GeminiProviderId AND ModelKey = @mKey;
            PRINT 'INFO: Modelo ' + @mKey + ' já existe (garantido como habilitado).';
        END

        FETCH NEXT FROM cur INTO @mKey, @mName;
    END

    CLOSE cur;
    DEALLOCATE cur;

    PRINT 'Atualização concluída com sucesso.';
END
ELSE
BEGIN
    PRINT 'ERRO: Provedor Gemini não encontrado na tabela AiProviders!';
END
