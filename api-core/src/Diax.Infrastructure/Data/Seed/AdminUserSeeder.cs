using Diax.Domain.Auth;
using Diax.Shared.Security;
using Microsoft.Extensions.Configuration;

namespace Diax.Infrastructure.Data.Seed;

public static class AdminUserSeeder
{
    public static void SeedInitialAdmin(DiaxDbContext db, IConfiguration configuration)
    {
        if (db.AdminUsers.Any())
            return;

        var adminEmail = configuration["Auth:AdminEmail"];
        var adminPassword = configuration["Auth:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var passwordHash = PasswordHash.HashPassword(adminPassword);
        var admin = new AdminUser(adminEmail.Trim(), passwordHash);

        db.AdminUsers.Add(admin);
        db.SaveChanges();
    }
}
