using Diax.Domain.AI;
using Diax.Domain.UserGroups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Data.Seeders;

/// <summary>
/// Seeder that automatically approves all video providers and their models for the admin group.
/// Runs idempotently on startup.
/// </summary>
public static class AdminGroupVideoAccessSeeder
{
    public static async Task ApproveVideoProvidersForAdminAsync(DiaxDbContext db, ILogger logger)
    {
        try
        {
            // Get admin group (created by UserSeeder)
            var adminGroup = await db.UserGroups
                .FirstOrDefaultAsync(g => g.Name == "Administradores");

            if (adminGroup == null)
            {
                logger.LogWarning("[AdminGroupVideoAccessSeeder] Admin group 'Administradores' not found");
                return;
            }

            // Get all video providers
            var videoProviders = await db.AiProviders
                .Where(p => p.IsVideoProvider)
                .ToListAsync();

            if (!videoProviders.Any())
            {
                logger.LogWarning("[AdminGroupVideoAccessSeeder] No video providers found in database");
                return;
            }

            int providerAccessCount = 0;
            int modelAccessCount = 0;

            // Grant provider-level access
            foreach (var provider in videoProviders)
            {
                var existingAccess = await db.GroupAiProviderAccesses
                    .FirstOrDefaultAsync(g => g.GroupId == adminGroup.Id && g.ProviderId == provider.Id);

                if (existingAccess == null)
                {
                    db.GroupAiProviderAccesses.Add(new GroupAiProviderAccess(
                        groupId: adminGroup.Id,
                        providerId: provider.Id));
                    providerAccessCount++;
                }

                // Grant model-level access (all models for this provider)
                var models = await db.AiModels
                    .Where(m => m.ProviderId == provider.Id)
                    .ToListAsync();

                foreach (var model in models)
                {
                    var existingModelAccess = await db.GroupAiModelAccesses
                        .FirstOrDefaultAsync(g => g.GroupId == adminGroup.Id && g.AiModelId == model.Id);

                    if (existingModelAccess == null)
                    {
                        db.GroupAiModelAccesses.Add(new GroupAiModelAccess(
                            groupId: adminGroup.Id,
                            aiModelId: model.Id));
                        modelAccessCount++;
                    }
                }
            }

            if (providerAccessCount > 0 || modelAccessCount > 0)
            {
                await db.SaveChangesAsync();
                logger.LogInformation(
                    "[AdminGroupVideoAccessSeeder] Approved {ProviderCount} provider(s) and {ModelCount} model(s) for admin group",
                    providerAccessCount, modelAccessCount);
            }
            else
            {
                logger.LogDebug("[AdminGroupVideoAccessSeeder] All video providers already approved for admin group");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[AdminGroupVideoAccessSeeder] Error approving video providers for admin group");
            throw;
        }
    }
}
