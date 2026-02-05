using Diax.Domain.Auth;
using Diax.Shared.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Data.Seed;

public static class AdminUserSeeder
{
    public static void SeedInitialAdmin(DiaxDbContext db, IConfiguration configuration, ILogger? logger = null)
    {
        logger?.LogInformation("AdminUserSeeder: checking if admin_users has any records...");

        if (db.AdminUsers.Any())
        {
            logger?.LogInformation("AdminUserSeeder: admin_users already has records, skipping seed.");
            return;
        }

        var adminEmail = configuration["Auth:AdminEmail"];
        var adminPassword = configuration["Auth:AdminPassword"];

        logger?.LogInformation("AdminUserSeeder: Auth:AdminEmail = '{Email}' (has value: {HasEmail})",
            string.IsNullOrWhiteSpace(adminEmail) ? "(empty)" : adminEmail,
            !string.IsNullOrWhiteSpace(adminEmail));
        logger?.LogInformation("AdminUserSeeder: Auth:AdminPassword has value: {HasPassword}",
            !string.IsNullOrWhiteSpace(adminPassword));

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger?.LogWarning("AdminUserSeeder: Auth:AdminEmail or Auth:AdminPassword is empty. Skipping seed.");
            return;
        }

        var passwordHash = PasswordHash.HashPassword(adminPassword);
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var admin = new AdminUser(adminEmail.Trim(), passwordHash, adminId, UserRole.Admin);

        db.AdminUsers.Add(admin);
        db.SaveChanges();

        logger?.LogInformation("AdminUserSeeder: Created initial admin user with email '{Email}'.", adminEmail);
    }
}
