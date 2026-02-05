using Diax.Domain.Auth;
using Diax.Domain.Auth.Enums;
using Diax.Shared.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Data.Seed;

public static class AdminUserSeeder
{
    public static void SeedInitialAdmin(DiaxDbContext db, IConfiguration configuration, ILogger? logger = null)
    {
        logger?.LogInformation("AdminUserSeeder: checking admin users...");

        var adminEmailFromConfig = configuration["Auth:AdminEmail"];
        var adminPasswordFromConfig = configuration["Auth:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmailFromConfig) || string.IsNullOrWhiteSpace(adminPasswordFromConfig))
        {
            logger?.LogWarning("AdminUserSeeder: Auth:AdminEmail or Auth:AdminPassword is empty. Skipping seed.");
            return;
        }

        var adminEmail = adminEmailFromConfig.Trim();
        var adminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var existingAdmin = db.AdminUsers.FirstOrDefault(x => x.Email == adminEmail || x.Id == adminId);

        if (existingAdmin != null)
        {
            logger?.LogInformation("AdminUserSeeder: Admin user already exists. Ensuring Role is Admin.");
            if (existingAdmin.Role != UserRole.Admin)
            {
                existingAdmin.SetRole(UserRole.Admin);
                db.SaveChanges();
                logger?.LogInformation("AdminUserSeeder: Updated existing admin role to Admin.");
            }
            return;
        }

        logger?.LogInformation("AdminUserSeeder: Creating initial admin user...");

        var passwordHash = PasswordHash.HashPassword(adminPasswordFromConfig);
        var admin = new AdminUser(adminEmail, passwordHash, UserRole.Admin, adminId);

        db.AdminUsers.Add(admin);
        db.SaveChanges();

        logger?.LogInformation("AdminUserSeeder: Created initial admin user with email '{Email}' and Role 'Admin'.", adminEmail);
    }
}
