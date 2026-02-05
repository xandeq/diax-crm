using Diax.Domain.Auth;
using Diax.Domain.Auth.Enums;
using Xunit;

namespace Diax.UnitTests.Auth;

public class AdminUserTests
{
    [Fact]
    public void Constructor_ShouldSetRole()
    {
        // Arrange
        var email = "test@example.com";
        var passwordHash = "hashhere";
        var role = UserRole.Admin;

        // Act
        var user = new AdminUser(email, passwordHash, role);

        // Assert
        Assert.Equal(role, user.Role);
    }

    [Fact]
    public void SetRole_ShouldUpdateRole()
    {
        // Arrange
        var user = new AdminUser("test@example.com", "hash", UserRole.User);

        // Act
        user.SetRole(UserRole.Admin);

        // Assert
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public void DefaultConstructor_ShouldSetDefaultRoleToUser()
    {
        // Arrange
        var user = new AdminUser("test@example.com", "hash");

        // Assert
        Assert.Equal(UserRole.User, user.Role);
    }
}
