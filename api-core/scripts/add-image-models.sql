-- ============================================================================
-- DIAX CRM: Add new image generation models and grant admin access
-- Run this script against the DiaxCRM database (SQL Server)
-- Safe to run multiple times (idempotent — uses IF NOT EXISTS checks)
-- ============================================================================

-- ────────────────────────────────────────────────────────────────────────────
-- 1. VERIFY PROVIDERS EXIST (display current state)
-- ────────────────────────────────────────────────────────────────────────────
SELECT id, [key], name, is_enabled FROM ai_providers;

-- ────────────────────────────────────────────────────────────────────────────
-- 2. ADD NEW MODELS
-- ────────────────────────────────────────────────────────────────────────────

-- Variables for provider IDs (adjust if your provider GUIDs are different)
DECLARE @geminiProviderId UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM ai_providers WHERE [key] = 'gemini');
DECLARE @falaiProviderId  UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM ai_providers WHERE [key] = 'falai');
DECLARE @openrouterProviderId UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM ai_providers WHERE [key] = 'openrouter');
DECLARE @openaiProviderId UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM ai_providers WHERE [key] = 'openai');
DECLARE @adminGroupId     UNIQUEIDENTIFIER = (SELECT TOP 1 id FROM user_groups WHERE [key] = 'system-admin');

-- ── Gemini: Nano Banana (gemini-2.0-flash-preview-image-generation) ─────────
-- This model already exists in KnownImageModelKeys; ensure it's in the DB.
IF @geminiProviderId IS NOT NULL
BEGIN
    -- gemini-2.0-flash-preview-image-generation
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @geminiProviderId AND model_key = 'gemini-2.0-flash-preview-image-generation')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, capabilities_json, created_at)
        VALUES (NEWID(), @geminiProviderId, 'gemini-2.0-flash-preview-image-generation', 'Gemini 2.0 Flash Image (Nano Banana)', 1, 0, '{"supportsImage":true}', GETUTCDATE());
        PRINT 'Added: gemini-2.0-flash-preview-image-generation';
    END
    ELSE
    BEGIN
        -- Ensure it's enabled
        UPDATE ai_models SET is_enabled = 1, display_name = 'Gemini 2.0 Flash Image (Nano Banana)'
        WHERE provider_id = @geminiProviderId AND model_key = 'gemini-2.0-flash-preview-image-generation';
        PRINT 'Updated: gemini-2.0-flash-preview-image-generation (enabled)';
    END

    -- gemini-2.5-flash-preview-image-generation (newer Nano Banana variant)
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @geminiProviderId AND model_key = 'gemini-2.5-flash-preview-image-generation')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, capabilities_json, created_at)
        VALUES (NEWID(), @geminiProviderId, 'gemini-2.5-flash-preview-image-generation', 'Gemini 2.5 Flash Image (Nano Banana 2)', 1, 0, '{"supportsImage":true}', GETUTCDATE());
        PRINT 'Added: gemini-2.5-flash-preview-image-generation';
    END

    -- gemini-2.0-flash-exp-image-generation (experimental, if not already there)
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @geminiProviderId AND model_key = 'gemini-2.0-flash-exp-image-generation')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, capabilities_json, created_at)
        VALUES (NEWID(), @geminiProviderId, 'gemini-2.0-flash-exp-image-generation', 'Gemini 2.0 Flash Image (Experimental)', 1, 0, '{"supportsImage":true}', GETUTCDATE());
        PRINT 'Added: gemini-2.0-flash-exp-image-generation';
    END
END
ELSE
    PRINT 'WARNING: Gemini provider not found! Add it first via Admin UI.';

-- ── Fal.ai: Kontext img2img models ─────────────────────────────────────────
IF @falaiProviderId IS NOT NULL
BEGIN
    -- fal-ai/flux-pro/kontext (Best for img2img editing)
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @falaiProviderId AND model_key = 'fal-ai/flux-pro/kontext')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, capabilities_json, created_at)
        VALUES (NEWID(), @falaiProviderId, 'fal-ai/flux-pro/kontext', 'FLUX Kontext Pro (img2img)', 1, 0, '{"supportsImage":true}', GETUTCDATE());
        PRINT 'Added: fal-ai/flux-pro/kontext';
    END

    -- fal-ai/flux-kontext/dev (Dev variant — cheaper)
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @falaiProviderId AND model_key = 'fal-ai/flux-kontext/dev')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, capabilities_json, created_at)
        VALUES (NEWID(), @falaiProviderId, 'fal-ai/flux-kontext/dev', 'FLUX Kontext Dev (img2img)', 1, 0, '{"supportsImage":true}', GETUTCDATE());
        PRINT 'Added: fal-ai/flux-kontext/dev';
    END

    -- fal-ai/flux/dev/image-to-image
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @falaiProviderId AND model_key = 'fal-ai/flux/dev/image-to-image')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, capabilities_json, created_at)
        VALUES (NEWID(), @falaiProviderId, 'fal-ai/flux/dev/image-to-image', 'FLUX Dev (img2img)', 1, 0, '{"supportsImage":true}', GETUTCDATE());
        PRINT 'Added: fal-ai/flux/dev/image-to-image';
    END

    -- fal-ai/fast-sdxl/image-to-image
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @falaiProviderId AND model_key = 'fal-ai/fast-sdxl/image-to-image')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, capabilities_json, created_at)
        VALUES (NEWID(), @falaiProviderId, 'fal-ai/fast-sdxl/image-to-image', 'SDXL Fast (img2img)', 1, 0, '{"supportsImage":true}', GETUTCDATE());
        PRINT 'Added: fal-ai/fast-sdxl/image-to-image';
    END
END
ELSE
    PRINT 'WARNING: Fal.ai provider not found! Add it first via Admin UI.';

-- ── OpenRouter: Gemini image models ─────────────────────────────────────────
IF @openrouterProviderId IS NOT NULL
BEGIN
    -- google/gemini-2.5-flash-image (Nano Banana via OpenRouter — supports img2img)
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @openrouterProviderId AND model_key = 'google/gemini-2.5-flash-image')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, capabilities_json, created_at)
        VALUES (NEWID(), @openrouterProviderId, 'google/gemini-2.5-flash-image', 'Gemini 2.5 Flash Image (via OpenRouter)', 1, 0, '{"supportsImage":true}', GETUTCDATE());
        PRINT 'Added: google/gemini-2.5-flash-image (OpenRouter)';
    END

    -- google/gemini-2.0-flash-image-generation
    IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @openrouterProviderId AND model_key = 'google/gemini-2.0-flash-image-generation')
    BEGIN
        INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered, capabilities_json, created_at)
        VALUES (NEWID(), @openrouterProviderId, 'google/gemini-2.0-flash-image-generation', 'Gemini 2.0 Flash Image (via OpenRouter)', 1, 0, '{"supportsImage":true}', GETUTCDATE());
        PRINT 'Added: google/gemini-2.0-flash-image-generation (OpenRouter)';
    END
END
ELSE
    PRINT 'WARNING: OpenRouter provider not found! Add it first via Admin UI.';


-- ────────────────────────────────────────────────────────────────────────────
-- 3. GRANT ADMIN GROUP ACCESS TO ALL NEW MODELS
-- ────────────────────────────────────────────────────────────────────────────
IF @adminGroupId IS NOT NULL
BEGIN
    -- Grant access to ALL enabled image models that admin doesn't have yet
    INSERT INTO group_ai_model_access (group_id, ai_model_id, created_at)
    SELECT @adminGroupId, m.id, GETUTCDATE()
    FROM ai_models m
    WHERE m.is_enabled = 1
      AND m.id NOT IN (
          SELECT gma.ai_model_id
          FROM group_ai_model_access gma
          WHERE gma.group_id = @adminGroupId
      );

    PRINT 'Granted admin access to all enabled models.';

    -- Also ensure admin has access to ALL providers
    INSERT INTO group_ai_provider_access (group_id, provider_id, created_at)
    SELECT @adminGroupId, p.id, GETUTCDATE()
    FROM ai_providers p
    WHERE p.is_enabled = 1
      AND p.id NOT IN (
          SELECT gpa.provider_id
          FROM group_ai_provider_access gpa
          WHERE gpa.group_id = @adminGroupId
      );

    PRINT 'Granted admin access to all enabled providers.';
END
ELSE
    PRINT 'WARNING: Admin group (system-admin) not found!';


-- ────────────────────────────────────────────────────────────────────────────
-- 4. VERIFY: Show all image models and their access status
-- ────────────────────────────────────────────────────────────────────────────
SELECT
    p.[key] AS provider,
    m.model_key,
    m.display_name,
    m.is_enabled,
    m.capabilities_json,
    CASE WHEN gma.ai_model_id IS NOT NULL THEN 'YES' ELSE 'NO' END AS admin_has_access
FROM ai_models m
JOIN ai_providers p ON m.provider_id = p.id
LEFT JOIN group_ai_model_access gma ON gma.ai_model_id = m.id AND gma.group_id = @adminGroupId
WHERE m.capabilities_json LIKE '%supportsImage%'
   OR m.model_key IN (
       'dall-e-3', 'dall-e-2', 'gpt-image-1',
       'gemini-2.0-flash-exp-image-generation', 'gemini-2.0-flash-preview-image-generation',
       'gemini-2.5-flash-preview-image-generation',
       'fal-ai/flux/dev', 'fal-ai/flux-pro', 'fal-ai/flux-pro/kontext',
       'fal-ai/flux-kontext/dev', 'fal-ai/flux/dev/image-to-image',
       'google/gemini-2.5-flash-image'
   )
ORDER BY p.[key], m.model_key;
