using Diax.Shared.Security;

namespace Diax.Tests.Security;

public class PasswordHashTests
{
    [Fact]
    public void HashPassword_ShouldProduceVerifiableHash()
    {
        var password = "Sup3rS3cret!";

        var hash = PasswordHash.HashPassword(password);

        Assert.NotEqual(password, hash);
        Assert.True(PasswordHash.Verify(hash, password));
    }

    [Fact]
    public void Verify_ShouldReturnFalse_ForWrongPassword()
    {
        var hash = PasswordHash.HashPassword("correct-password");

        var verified = PasswordHash.Verify(hash, "wrong-password");

        Assert.False(verified);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("PBKDF2$abc$salt$hash")]
    public void Verify_ShouldReturnFalse_ForMalformedHash(string malformedHash)
    {
        var verified = PasswordHash.Verify(malformedHash, "any-password");

        Assert.False(verified);
    }
}
