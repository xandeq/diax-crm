-- =============================================================================
-- DIAGNÓSTICO E CORREÇÃO: Image Generation - Gemini + Fal.ai + OpenAI
-- Executar no SQL Server conectado ao banco DiaxCRM (produção)
-- =============================================================================

-- PASSO 1: VER O ESTADO ATUAL DOS PROVIDERS
-- =============================================================================
SELECT
    p.Id,
    p.[Key],
    p.[Name],
    p.IsEnabled,
    COUNT(m.Id) AS TotalModels,
    SUM(CASE WHEN m.IsEnabled = 1 THEN 1 ELSE 0 END) AS EnabledModels
FROM ai_providers p
LEFT JOIN ai_models m ON m.ProviderId = p.Id
WHERE p.[Key] IN ('openai', 'gemini', 'falai')
GROUP BY p.Id, p.[Key], p.[Name], p.IsEnabled
ORDER BY p.[Key];

-- =============================================================================
-- PASSO 2: VER OS MODELOS DE IMAGEM EXISTENTES POR PROVIDER
-- =============================================================================
SELECT
    p.[Key] AS Provider,
    m.ModelKey,
    m.DisplayName,
    m.IsEnabled,
    m.IsDiscovered,
    m.CapabilitiesJson
FROM ai_models m
JOIN ai_providers p ON p.Id = m.ProviderId
WHERE p.[Key] IN ('openai', 'gemini', 'falai')
ORDER BY p.[Key], m.ModelKey;

-- =============================================================================
-- PASSO 3: VER PERMISSÕES DO GRUPO ADMIN
-- (Troque o GUID pelo Id real do grupo Admin se necessário)
-- =============================================================================
DECLARE @AdminGroupId UNIQUEIDENTIFIER = '2EC26639-1370-4C94-ADFB-D10F0CAA5EC0';

SELECT
    'PROVIDER ACCESS' AS Type,
    p.[Key] AS Resource,
    pa.IsEnabled
FROM group_ai_provider_access pa
JOIN ai_providers p ON p.Id = pa.ProviderId
WHERE pa.GroupId = @AdminGroupId

UNION ALL

SELECT
    'MODEL ACCESS' AS Type,
    CONCAT(pr.[Key], '/', m.ModelKey) AS Resource,
    ma.IsEnabled
FROM group_ai_model_access ma
JOIN ai_models m ON m.Id = ma.ModelId
JOIN ai_providers pr ON pr.Id = m.ProviderId
WHERE ma.GroupId = @AdminGroupId
ORDER BY Type, Resource;


-- =============================================================================
-- PASSO 4: CORRIGIR — Garantir que Fal.ai provider está habilitado
-- =============================================================================
UPDATE ai_providers SET IsEnabled = 1 WHERE [Key] = 'falai';
UPDATE ai_providers SET IsEnabled = 1 WHERE [Key] = 'gemini';
UPDATE ai_providers SET IsEnabled = 1 WHERE [Key] = 'openai';

-- =============================================================================
-- PASSO 5: ADICIONAR MODELOS FAL.AI SE NÃO EXISTIREM
-- =============================================================================
DECLARE @FalaiProviderId UNIQUEIDENTIFIER = (SELECT Id FROM ai_providers WHERE [Key] = 'falai');
DECLARE @GeminiProviderId UNIQUEIDENTIFIER = (SELECT Id FROM ai_providers WHERE [Key] = 'gemini');
DECLARE @OpenAiProviderId UNIQUEIDENTIFIER = (SELECT Id FROM ai_providers WHERE [Key] = 'openai');

-- Fal.ai: Flux Dev
IF NOT EXISTS (SELECT 1 FROM ai_models WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/flux/dev')
    INSERT INTO ai_models (Id, ProviderId, ModelKey, DisplayName, IsEnabled, IsDiscovered, CapabilitiesJson, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @FalaiProviderId, 'fal-ai/flux/dev', 'Flux Dev', 1, 1, '{"supportsImage":true}', GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE ai_models SET IsEnabled = 1 WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/flux/dev';

-- Fal.ai: Flux Dev Image-to-Image
IF NOT EXISTS (SELECT 1 FROM ai_models WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/flux/dev/image-to-image')
    INSERT INTO ai_models (Id, ProviderId, ModelKey, DisplayName, IsEnabled, IsDiscovered, CapabilitiesJson, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @FalaiProviderId, 'fal-ai/flux/dev/image-to-image', 'Flux Dev (Image-to-Image)', 1, 1, '{"supportsImage":true}', GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE ai_models SET IsEnabled = 1 WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/flux/dev/image-to-image';

-- Fal.ai: Fast SDXL
IF NOT EXISTS (SELECT 1 FROM ai_models WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/fast-sdxl')
    INSERT INTO ai_models (Id, ProviderId, ModelKey, DisplayName, IsEnabled, IsDiscovered, CapabilitiesJson, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @FalaiProviderId, 'fal-ai/fast-sdxl', 'Fast SDXL', 1, 1, '{"supportsImage":true}', GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE ai_models SET IsEnabled = 1 WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/fast-sdxl';

-- Fal.ai: Flux Pro v1.1
IF NOT EXISTS (SELECT 1 FROM ai_models WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/flux-pro/v1.1')
    INSERT INTO ai_models (Id, ProviderId, ModelKey, DisplayName, IsEnabled, IsDiscovered, CapabilitiesJson, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @FalaiProviderId, 'fal-ai/flux-pro/v1.1', 'Flux Pro v1.1', 1, 1, '{"supportsImage":true}', GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE ai_models SET IsEnabled = 1 WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/flux-pro/v1.1';

-- Fal.ai: Luma Dream Machine
IF NOT EXISTS (SELECT 1 FROM ai_models WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/luma-dream-machine')
    INSERT INTO ai_models (Id, ProviderId, ModelKey, DisplayName, IsEnabled, IsDiscovered, CapabilitiesJson, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @FalaiProviderId, 'fal-ai/luma-dream-machine', 'Luma Dream Machine', 1, 1, '{"supportsImage":true}', GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE ai_models SET IsEnabled = 1 WHERE ProviderId = @FalaiProviderId AND ModelKey = 'fal-ai/luma-dream-machine';

-- Gemini: Flash Image Generation (modelo ativo para geração de imagens em 2026)
IF NOT EXISTS (SELECT 1 FROM ai_models WHERE ProviderId = @GeminiProviderId AND ModelKey = 'gemini-2.0-flash-preview-image-generation')
    INSERT INTO ai_models (Id, ProviderId, ModelKey, DisplayName, IsEnabled, IsDiscovered, CapabilitiesJson, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @GeminiProviderId, 'gemini-2.0-flash-preview-image-generation', 'Gemini Flash (Image Generation)', 1, 1, '{"supportsImage":true}', GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE ai_models SET IsEnabled = 1 WHERE ProviderId = @GeminiProviderId AND ModelKey = 'gemini-2.0-flash-preview-image-generation';

-- Gemini: Gemini 2.0 Flash Exp Image Generation (nome alternativo)
IF NOT EXISTS (SELECT 1 FROM ai_models WHERE ProviderId = @GeminiProviderId AND ModelKey = 'gemini-2.0-flash-exp-image-generation')
    INSERT INTO ai_models (Id, ProviderId, ModelKey, DisplayName, IsEnabled, IsDiscovered, CapabilitiesJson, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @GeminiProviderId, 'gemini-2.0-flash-exp-image-generation', 'Gemini Flash Exp (Image Generation)', 1, 1, '{"supportsImage":true}', GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE ai_models SET IsEnabled = 1 WHERE ProviderId = @GeminiProviderId AND ModelKey = 'gemini-2.0-flash-exp-image-generation';

-- OpenAI: Garantir DALL-E 3 habilitado
IF NOT EXISTS (SELECT 1 FROM ai_models WHERE ProviderId = @OpenAiProviderId AND ModelKey = 'dall-e-3')
    INSERT INTO ai_models (Id, ProviderId, ModelKey, DisplayName, IsEnabled, IsDiscovered, CapabilitiesJson, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @OpenAiProviderId, 'dall-e-3', 'DALL-E 3', 1, 1, '{"supportsImage":true}', GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE ai_models SET IsEnabled = 1 WHERE ProviderId = @OpenAiProviderId AND ModelKey = 'dall-e-3';

-- OpenAI: Garantir DALL-E 2 habilitado
IF NOT EXISTS (SELECT 1 FROM ai_models WHERE ProviderId = @OpenAiProviderId AND ModelKey = 'dall-e-2')
    INSERT INTO ai_models (Id, ProviderId, ModelKey, DisplayName, IsEnabled, IsDiscovered, CapabilitiesJson, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @OpenAiProviderId, 'dall-e-2', 'DALL-E 2', 1, 1, '{"supportsImage":true}', GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE ai_models SET IsEnabled = 1 WHERE ProviderId = @OpenAiProviderId AND ModelKey = 'dall-e-2';


-- =============================================================================
-- PASSO 6: CONCEDER ACESSO AO GRUPO ADMIN (provider + todos os modelos)
-- =============================================================================

-- Provider: Fal.ai
IF NOT EXISTS (SELECT 1 FROM group_ai_provider_access WHERE GroupId = @AdminGroupId AND ProviderId = @FalaiProviderId)
    INSERT INTO group_ai_provider_access (Id, GroupId, ProviderId, IsEnabled, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @AdminGroupId, @FalaiProviderId, 1, GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE group_ai_provider_access SET IsEnabled = 1
    WHERE GroupId = @AdminGroupId AND ProviderId = @FalaiProviderId;

-- Provider: Gemini (garantir acesso)
IF NOT EXISTS (SELECT 1 FROM group_ai_provider_access WHERE GroupId = @AdminGroupId AND ProviderId = @GeminiProviderId)
    INSERT INTO group_ai_provider_access (Id, GroupId, ProviderId, IsEnabled, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @AdminGroupId, @GeminiProviderId, 1, GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE group_ai_provider_access SET IsEnabled = 1
    WHERE GroupId = @AdminGroupId AND ProviderId = @GeminiProviderId;

-- Provider: OpenAI (garantir acesso)
IF NOT EXISTS (SELECT 1 FROM group_ai_provider_access WHERE GroupId = @AdminGroupId AND ProviderId = @OpenAiProviderId)
    INSERT INTO group_ai_provider_access (Id, GroupId, ProviderId, IsEnabled, CreatedAt, UpdatedAt)
    VALUES (NEWID(), @AdminGroupId, @OpenAiProviderId, 1, GETUTCDATE(), GETUTCDATE());
ELSE
    UPDATE group_ai_provider_access SET IsEnabled = 1
    WHERE GroupId = @AdminGroupId AND ProviderId = @OpenAiProviderId;

-- Acesso a TODOS os modelos habilitados dos 3 providers para o grupo Admin
INSERT INTO group_ai_model_access (Id, GroupId, ModelId, IsEnabled, CreatedAt, UpdatedAt)
SELECT
    NEWID(),
    @AdminGroupId,
    m.Id,
    1,
    GETUTCDATE(),
    GETUTCDATE()
FROM ai_models m
JOIN ai_providers p ON p.Id = m.ProviderId
WHERE p.[Key] IN ('openai', 'gemini', 'falai')
  AND m.IsEnabled = 1
  AND NOT EXISTS (
    SELECT 1 FROM group_ai_model_access
    WHERE GroupId = @AdminGroupId AND ModelId = m.Id
  );

-- Garantir que os registros existentes também estão habilitados
UPDATE group_ai_model_access
SET IsEnabled = 1
FROM group_ai_model_access ma
JOIN ai_models m ON m.Id = ma.ModelId
JOIN ai_providers p ON p.Id = m.ProviderId
WHERE ma.GroupId = @AdminGroupId
  AND p.[Key] IN ('openai', 'gemini', 'falai');

-- =============================================================================
-- PASSO 7: VERIFICAÇÃO FINAL — Ver o que o grupo Admin tem acesso
-- =============================================================================
SELECT
    p.[Key] AS Provider,
    m.ModelKey,
    m.DisplayName,
    m.IsEnabled AS ModelEnabled,
    CASE WHEN pa.IsEnabled = 1 THEN 'SIM' ELSE 'NÃO' END AS ProviderAccess,
    CASE WHEN ma.IsEnabled = 1 THEN 'SIM' ELSE 'NÃO' END AS ModelAccess
FROM ai_models m
JOIN ai_providers p ON p.Id = m.ProviderId
LEFT JOIN group_ai_provider_access pa ON pa.ProviderId = p.Id AND pa.GroupId = @AdminGroupId
LEFT JOIN group_ai_model_access ma ON ma.ModelId = m.Id AND ma.GroupId = @AdminGroupId
WHERE p.[Key] IN ('openai', 'gemini', 'falai')
ORDER BY p.[Key], m.ModelKey;

-- =============================================================================
-- PASSO EXTRA: Corrigir modelos existentes sem CapabilitiesJson
-- (modelos inseridos antes desta correção ficaram sem supportsImage:true)
-- =============================================================================
UPDATE ai_models
SET CapabilitiesJson = '{"supportsImage":true}',
    UpdatedAt = GETUTCDATE()
FROM ai_models m
JOIN ai_providers p ON p.Id = m.ProviderId
WHERE p.[Key] IN ('openai', 'gemini', 'falai')
  AND (m.CapabilitiesJson IS NULL OR m.CapabilitiesJson NOT LIKE '%supportsImage%');

-- Confirmar resultado
SELECT p.[Key] AS Provider, m.ModelKey, m.IsEnabled, m.CapabilitiesJson
FROM ai_models m
JOIN ai_providers p ON p.Id = m.ProviderId
WHERE p.[Key] IN ('openai', 'gemini', 'falai')
ORDER BY p.[Key], m.ModelKey;
