using Diax.Domain.Common;

namespace Diax.Domain.Auth;

/// <summary>
/// Entidade de usuário do sistema.
/// Substitui AdminUser. Papéis são definidos via UserGroups/Permissions (RBAC).
/// </summary>
public class User : AuditableEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // EF Core
    private User() { }

    public User(string email, string passwordHash, Guid? id = null) : base(id ?? Guid.NewGuid())
    {
        SetEmail(email);
        SetPasswordHash(passwordHash);
        IsActive = true;
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        Email = email.Trim().ToLowerInvariant();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    public void Disable() => IsActive = false;
    public void Enable() => IsActive = true;
}
