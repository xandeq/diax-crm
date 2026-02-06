-- Script para corrigir e garantir os modelos Gemini na produção
-- Ajustado para usar snake_case e chave 'gemini'

-- 1. Obter o ID do provedor Google (usando a chave 'gemini')
DECLARE @GoogleProviderId UNIQUEIDENTIFIER;
SELECT @GoogleProviderId = id FROM ai_providers WHERE [key] = 'gemini';

IF @GoogleProviderId IS NOT NULL
BEGIN
    -- 2. Garantir que gemini-2.5-flash exista e esteja ativo
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE model_key = 'gemini-2.5-flash')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, created_at, created_by, updated_at, updated_by)
        VALUES (NEWID(), @GoogleProviderId, 'gemini-2.5-flash', 'Gemini 2.5 Flash', 1, 0, GETDATE(), 'system', GETDATE(), 'system');
    END
    ELSE
    BEGIN
        UPDATE ai_models SET is_enabled = 1 WHERE model_key = 'gemini-2.5-flash';
    END

    -- 3. Garantir que gemini-2.0-flash exista e esteja ativo
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE model_key = 'gemini-2.0-flash')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, created_at, created_by, updated_at, updated_by)
        VALUES (NEWID(), @GoogleProviderId, 'gemini-2.0-flash', 'Gemini 2.0 Flash', 1, 0, GETDATE(), 'system', GETDATE(), 'system');
    END
    ELSE
    BEGIN
        UPDATE ai_models SET is_enabled = 1 WHERE model_key = 'gemini-2.0-flash';
    END

    -- 4. Re-ativar gemini-1.5-flash se ele estiver desativado
    UPDATE ai_models SET is_enabled = 1 WHERE model_key = 'gemini-1.5-flash';

    PRINT 'Sucesso: Modelos Gemini atualizados.';
END
ELSE
BEGIN
    PRINT 'Erro: Provedor com chave ''gemini'' não encontrado na tabela ai_providers.';
END
