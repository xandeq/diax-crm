using Diax.Domain.Auth;
using Diax.Domain.UserGroups;
using Diax.Shared.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Diax.Infrastructure.Data.Seed;

public static class UserSeeder
{
    public static void SeedInitialAdmin(DiaxDbContext db, IConfiguration configuration, ILogger? logger = null)
    {
        logger?.LogInformation("UserSeeder: checking admin users...");

        var adminEmailFromConfig = configuration["Auth:AdminEmail"];
        var adminPasswordFromConfig = configuration["Auth:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmailFromConfig) || string.IsNullOrWhiteSpace(adminPasswordFromConfig))
        {
            logger?.LogWarning("UserSeeder: Auth:AdminEmail or Auth:AdminPassword is empty. Skipping seed.");
            return;
        }

        var adminEmail = adminEmailFromConfig.Trim().ToLowerInvariant();
        var seedAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // 1. Garantir usuário admin existe
        var existingAdmin = db.Users.FirstOrDefault(x => x.Email == adminEmail || x.Id == seedAdminId);

        Guid adminId;
        if (existingAdmin == null)
        {
            logger?.LogInformation("UserSeeder: Creating initial admin user...");
            var passwordHash = PasswordHash.HashPassword(adminPasswordFromConfig);
            var admin = new User(adminEmail, passwordHash, seedAdminId);
            db.Users.Add(admin);
            db.SaveChanges();
            adminId = seedAdminId;
            logger?.LogInformation("UserSeeder: Created initial admin user with email '{Email}'.", adminEmail);
        }
        else
        {
            adminId = existingAdmin.Id;
            logger?.LogInformation("UserSeeder: Admin user already exists (ID: {Id}).", adminId);
            if (!existingAdmin.IsActive)
            {
                existingAdmin.Enable();
                db.SaveChanges();
                logger?.LogInformation("UserSeeder: Re-enabled admin user.");
            }
        }

        // 2. Garantir grupo system-admin existe
        var adminGroup = db.UserGroups.FirstOrDefault(g => g.Key == "system-admin");
        if (adminGroup == null)
        {
            adminGroup = new UserGroup("system-admin", "Administradores", true, "Acesso total ao sistema");
            db.UserGroups.Add(adminGroup);
            db.SaveChanges();
            logger?.LogInformation("UserSeeder: Created system-admin group.");
        }

        // 3. Garantir admin está no grupo
        var membership = db.UserGroupMembers.FirstOrDefault(
            m => m.UserId == adminId && m.GroupId == adminGroup.Id);

        if (membership == null)
        {
            adminGroup.AddMember(adminId);
            db.SaveChanges();
            logger?.LogInformation("UserSeeder: Added admin to system-admin group.");
        }
    }
}
