namespace IGBZ.Application.Tests.Fakes;

using IGBZ.Application.Abstractions;

/// <summary>رمزنگاری هویتی برای تست — مقدار خام را دست‌نخورده برمی‌گرداند.</summary>
public class FakeEncryptionService : IEncryptionService
{
    public static readonly FakeEncryptionService Instance = new();

    public string Encrypt(string plaintext) => plaintext;

    public string? Decrypt(string ciphertext) => ciphertext;
}
