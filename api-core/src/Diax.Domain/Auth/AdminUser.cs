using Diax.Domain.Auth.Enums;
using Diax.Domain.Common;

namespace Diax.Domain.Auth;

public class AdminUser : AuditableEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public UserRole Role { get; private set; } = UserRole.User;

    // EF Core
    private AdminUser() { }

    public AdminUser(string email, string passwordHash, UserRole role = UserRole.User, Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        SetEmail(email);
        SetPasswordHash(passwordHash);
        Role = role;
        IsActive = true;
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        Email = email.Trim();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    public void SetRole(UserRole role)
    {
        Role = role;
    }

    public void Disable() => IsActive = false;
    public void Enable() => IsActive = true;
}
