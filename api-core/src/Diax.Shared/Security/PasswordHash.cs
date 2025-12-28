using System.Security.Cryptography;

namespace Diax.Shared.Security;

public static class PasswordHash
{
    private const string Scheme = "PBKDF2";
    private const int SaltSize = 16; // 128-bit
    private const int KeySize = 32;  // 256-bit
    private const int DefaultIterations = 100_000;

    public static string HashPassword(string password, int iterations = DefaultIterations)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password is required.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: KeySize);

        return string.Join('$',
            Scheme,
            iterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public static bool Verify(string storedHash, string password)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || password is null)
            return false;

        var parts = storedHash.Split('$');
        if (parts.Length != 4)
            return false;

        if (!string.Equals(parts[0], Scheme, StringComparison.Ordinal))
            return false;

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
            return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
