namespace Diax.Application.Common;

/// <summary>
/// Provides access to the currently authenticated user's information.
/// Used for multi-tenant data isolation in the financial module.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the ID of the currently authenticated user.
    /// Returns null if no user is authenticated.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the email of the currently authenticated user.
    /// Returns null if no user is authenticated.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Indicates whether there is an authenticated user.
    /// </summary>
    bool IsAuthenticated { get; }
}
