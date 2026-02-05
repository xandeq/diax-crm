using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGeminiModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. Obter o ID do provider Gemini
                DECLARE @geminiProviderId UNIQUEIDENTIFIER;
                SELECT @geminiProviderId = id FROM ai_providers WHERE [key] = 'google' OR [key] = 'gemini';

                IF @geminiProviderId IS NULL
                BEGIN
                    -- Se o provider não existir, criar
                    SET @geminiProviderId = NEWID();
                    INSERT INTO ai_providers (id, [key], name, supports_list_models, base_url, is_enabled)
                    VALUES (@geminiProviderId, 'google', 'Google Gemini', 1, 'https://generativelanguage.googleapis.com', 1);
                END

                -- 2. Desabilitar modelos obsoletos do Gemini
                UPDATE ai_models
                SET is_enabled = 0
                WHERE provider_id = @geminiProviderId
                AND model_key IN (
                    'gemini-2.0-flash-exp',
                    'gemini-exp-1206',
                    'gemini-1.5-pro',
                    'gemini-1.5-flash'
                );

                -- 3. Inserir/Atualizar modelos oficiais (operações idempotentes)

                -- gemini-2.5-flash
                IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @geminiProviderId AND model_key = 'gemini-2.5-flash')
                    INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered)
                    VALUES (NEWID(), @geminiProviderId, 'gemini-2.5-flash', 'Gemini 2.5 Flash', 1, 0);
                ELSE
                    UPDATE ai_models SET is_enabled = 1, display_name = 'Gemini 2.5 Flash'
                    WHERE provider_id = @geminiProviderId AND model_key = 'gemini-2.5-flash';

                -- gemini-2.0-flash
                IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @geminiProviderId AND model_key = 'gemini-2.0-flash')
                    INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered)
                    VALUES (NEWID(), @geminiProviderId, 'gemini-2.0-flash', 'Gemini 2.0 Flash', 1, 0);
                ELSE
                    UPDATE ai_models SET is_enabled = 1, display_name = 'Gemini 2.0 Flash'
                    WHERE provider_id = @geminiProviderId AND model_key = 'gemini-2.0-flash';

                -- gemini-flash-latest
                IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @geminiProviderId AND model_key = 'gemini-flash-latest')
                    INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered)
                    VALUES (NEWID(), @geminiProviderId, 'gemini-flash-latest', 'Gemini Flash Latest', 1, 0);
                ELSE
                    UPDATE ai_models SET is_enabled = 1, display_name = 'Gemini Flash Latest'
                    WHERE provider_id = @geminiProviderId AND model_key = 'gemini-flash-latest';

                -- gemini-pro-latest
                IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @geminiProviderId AND model_key = 'gemini-pro-latest')
                    INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered)
                    VALUES (NEWID(), @geminiProviderId, 'gemini-pro-latest', 'Gemini Pro Latest', 1, 0);
                ELSE
                    UPDATE ai_models SET is_enabled = 1, display_name = 'Gemini Pro Latest'
                    WHERE provider_id = @geminiProviderId AND model_key = 'gemini-pro-latest';

                -- gemma-3-4b-it
                IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @geminiProviderId AND model_key = 'gemma-3-4b-it')
                    INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered)
                    VALUES (NEWID(), @geminiProviderId, 'gemma-3-4b-it', 'Gemma 3 4B IT', 1, 0);
                ELSE
                    UPDATE ai_models SET is_enabled = 1, display_name = 'Gemma 3 4B IT'
                    WHERE provider_id = @geminiProviderId AND model_key = 'gemma-3-4b-it';

                -- gemma-3-12b-it
                IF NOT EXISTS (SELECT 1 FROM ai_models WHERE provider_id = @geminiProviderId AND model_key = 'gemma-3-12b-it')
                    INSERT INTO ai_models (id, provider_id, model_key, display_name, is_enabled, is_discovered)
                    VALUES (NEWID(), @geminiProviderId, 'gemma-3-12b-it', 'Gemma 3 12B IT', 1, 0);
                ELSE
                    UPDATE ai_models SET is_enabled = 1, display_name = 'Gemma 3 12B IT'
                    WHERE provider_id = @geminiProviderId AND model_key = 'gemma-3-12b-it';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Reverter: reabilitar modelos antigos e desabilitar novos
                DECLARE @geminiProviderId UNIQUEIDENTIFIER;
                SELECT @geminiProviderId = id FROM ai_providers WHERE [key] = 'google' OR [key] = 'gemini';

                IF @geminiProviderId IS NOT NULL
                BEGIN
                    -- Reabilitar modelos antigos
                    UPDATE ai_models
                    SET is_enabled = 1
                    WHERE provider_id = @geminiProviderId
                    AND model_key IN ('gemini-2.0-flash-exp', 'gemini-exp-1206', 'gemini-1.5-pro', 'gemini-1.5-flash');

                    -- Desabilitar modelos novos
                    UPDATE ai_models
                    SET is_enabled = 0
                    WHERE provider_id = @geminiProviderId
                    AND model_key IN ('gemini-2.5-flash', 'gemini-flash-latest', 'gemini-pro-latest', 'gemma-3-4b-it', 'gemma-3-12b-it');
                END
            ");
        }
    }
}
