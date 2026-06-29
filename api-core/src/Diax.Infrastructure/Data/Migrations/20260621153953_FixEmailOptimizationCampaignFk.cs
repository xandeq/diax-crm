using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Diax.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixEmailOptimizationCampaignFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: prod already has the desired end-state (email_campaign_id uniqueidentifier,
            // proper FK/index, no shadow *_id1 objects). Each block is guarded so it is a no-op when
            // the target object is already absent or already in the correct form.

            // 1. Drop shadow FK (exists only on dev if EF created a duplicate navigation)
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_email_optimizations_email_campaigns_email_campaign_id1'
                      AND parent_object_id = OBJECT_ID('email_optimizations')
                )
                    ALTER TABLE [email_optimizations]
                        DROP CONSTRAINT [FK_email_optimizations_email_campaigns_email_campaign_id1];
            ");

            // 2. Drop shadow index
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_email_optimizations_email_campaign_id1'
                      AND object_id = OBJECT_ID('email_optimizations')
                )
                    DROP INDEX [IX_email_optimizations_email_campaign_id1]
                        ON [email_optimizations];
            ");

            // 3. Drop shadow column
            migrationBuilder.Sql(@"
                IF COL_LENGTH('email_optimizations', 'email_campaign_id1') IS NOT NULL
                    ALTER TABLE [email_optimizations] DROP COLUMN [email_campaign_id1];
            ");

            // 4. If email_campaign_id is still the old bigint type, drop and re-add as
            //    uniqueidentifier. SQL Server does not allow ALTER COLUMN between these types.
            //    Safe because email_optimizations has 0 rows in prod (and is never persisted).
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME   = 'email_optimizations'
                      AND COLUMN_NAME  = 'email_campaign_id'
                      AND DATA_TYPE    = 'bigint'
                )
                BEGIN
                    -- Remove dependent index/FK on the bigint column before dropping it
                    IF EXISTS (
                        SELECT 1 FROM sys.foreign_keys
                        WHERE name = 'FK_email_optimizations_email_campaigns_email_campaign_id'
                          AND parent_object_id = OBJECT_ID('email_optimizations')
                    )
                        ALTER TABLE [email_optimizations]
                            DROP CONSTRAINT [FK_email_optimizations_email_campaigns_email_campaign_id];

                    IF EXISTS (
                        SELECT 1 FROM sys.indexes
                        WHERE name = 'IX_email_optimizations_email_campaign_id'
                          AND object_id = OBJECT_ID('email_optimizations')
                    )
                        DROP INDEX [IX_email_optimizations_email_campaign_id]
                            ON [email_optimizations];

                    ALTER TABLE [email_optimizations] DROP COLUMN [email_campaign_id];
                    ALTER TABLE [email_optimizations] ADD [email_campaign_id] [uniqueidentifier] NULL;
                END
            ");

            // 5. Ensure index exists (no-op if already present)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_email_optimizations_email_campaign_id'
                      AND object_id = OBJECT_ID('email_optimizations')
                )
                    CREATE INDEX [IX_email_optimizations_email_campaign_id]
                        ON [email_optimizations] ([email_campaign_id]);
            ");

            // 6. Ensure FK exists (no-op if already present)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_email_optimizations_email_campaigns_email_campaign_id'
                      AND parent_object_id = OBJECT_ID('email_optimizations')
                )
                    ALTER TABLE [email_optimizations]
                        ADD CONSTRAINT [FK_email_optimizations_email_campaigns_email_campaign_id]
                        FOREIGN KEY ([email_campaign_id]) REFERENCES [email_campaigns] ([id]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Idempotent rollback: restores the shadow *_id1 objects and bigint column.
            // Each block is guarded so it is safe even if the object is already absent/present.

            // 1. Drop current proper FK
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_email_optimizations_email_campaigns_email_campaign_id'
                      AND parent_object_id = OBJECT_ID('email_optimizations')
                )
                    ALTER TABLE [email_optimizations]
                        DROP CONSTRAINT [FK_email_optimizations_email_campaigns_email_campaign_id];
            ");

            // 2. Drop current index
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_email_optimizations_email_campaign_id'
                      AND object_id = OBJECT_ID('email_optimizations')
                )
                    DROP INDEX [IX_email_optimizations_email_campaign_id]
                        ON [email_optimizations];
            ");

            // 3. If email_campaign_id is uniqueidentifier, revert to bigint
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME   = 'email_optimizations'
                      AND COLUMN_NAME  = 'email_campaign_id'
                      AND DATA_TYPE    = 'uniqueidentifier'
                )
                BEGIN
                    ALTER TABLE [email_optimizations] DROP COLUMN [email_campaign_id];
                    ALTER TABLE [email_optimizations] ADD [email_campaign_id] [bigint] NULL;
                END
            ");

            // 4. Restore shadow column if not present
            migrationBuilder.Sql(@"
                IF COL_LENGTH('email_optimizations', 'email_campaign_id1') IS NULL
                    ALTER TABLE [email_optimizations]
                        ADD [email_campaign_id1] [uniqueidentifier] NULL;
            ");

            // 5. Restore shadow index
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_email_optimizations_email_campaign_id1'
                      AND object_id = OBJECT_ID('email_optimizations')
                )
                    CREATE INDEX [IX_email_optimizations_email_campaign_id1]
                        ON [email_optimizations] ([email_campaign_id1]);
            ");

            // 6. Restore shadow FK
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_email_optimizations_email_campaigns_email_campaign_id1'
                      AND parent_object_id = OBJECT_ID('email_optimizations')
                )
                    ALTER TABLE [email_optimizations]
                        ADD CONSTRAINT [FK_email_optimizations_email_campaigns_email_campaign_id1]
                        FOREIGN KEY ([email_campaign_id1]) REFERENCES [email_campaigns] ([id]);
            ");
        }
    }
}
