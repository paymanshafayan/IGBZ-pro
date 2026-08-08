namespace IGBZ.Infrastructure.Tests;

using IGBZ.Infrastructure.Security;
using Xunit;

public class EncryptionServiceTests
{
    private const string Key = "ZGV2LW9ubHktZW5jcnlwdGlvbi1rZXktMDEyMzQ1Njc4OWFiY2RlZg=="; // 32 بایت base64

    [Fact]
    public void Encrypt_ThenDecrypt_RoundTrips()
    {
        var service = new EncryptionService(Key);

        var encrypted = service.Encrypt("my-secret-api-key");
        var decrypted = service.Decrypt(encrypted);

        Assert.NotEqual("my-secret-api-key", encrypted); // واقعاً رمز شده
        Assert.StartsWith("enc:", encrypted);
        Assert.Equal("my-secret-api-key", decrypted);
    }

    [Fact]
    public void Encrypt_SameValue_ProducesDifferentCiphertext()
    {
        var service = new EncryptionService(Key);

        var a = service.Encrypt("same");
        var b = service.Encrypt("same");

        Assert.NotEqual(a, b); // nonce تصادفی
    }

    [Fact]
    public void Decrypt_LegacyPlaintext_ReturnsAsIs()
    {
        var service = new EncryptionService(Key);

        Assert.Equal("plain-key", service.Decrypt("plain-key"));
    }

    [Fact]
    public void Decrypt_WrongKey_ReturnsNull()
    {
        var service = new EncryptionService(Key);
        var encrypted = service.Encrypt("secret");

        var wrong = new EncryptionService("d3dyb25nbHktZW5jcnlwdGlvbi1rZXktOTg3NjU0MzIxMGFiY2RlZg==");
        Assert.Null(wrong.Decrypt(encrypted));
    }

    [Fact]
    public void Decrypt_InvalidData_ReturnsNull()
    {
        var service = new EncryptionService(Key);
        Assert.Null(service.Decrypt("enc:%%%invalid%%%"));
    }

    [Fact]
    public void Constructor_InvalidKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => new EncryptionService(""));
        Assert.Throws<ArgumentException>(() => new EncryptionService("not-base64!"));
        Assert.Throws<ArgumentException>(() => new EncryptionService(Convert.ToBase64String(new byte[16]))); // ۱۶ بایت نه ۳۲
    }
}
