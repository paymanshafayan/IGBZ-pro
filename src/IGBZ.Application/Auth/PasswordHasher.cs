namespace IGBZ.Application.Auth;

using System.Security.Cryptography;

/// <summary>
/// هش رمز عبور با PBKDF2 (Rfc2898DeriveBytes) + Salt تصادفی — بدون وابستگی خارجی.
/// فرمت ذخیره: {iterations}.{saltBase64}.{hashBase64}
/// </summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("رمز عبور نمی‌تواند خالی باشد.", nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        var parts = storedHash.Split('.');
        if (parts.Length != 3)
            return false;

        if (!int.TryParse(parts[0], out var iterations) || iterations < 1)
            return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expectedHash = Convert.FromBase64String(parts[2]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
