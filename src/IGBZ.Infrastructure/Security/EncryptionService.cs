namespace IGBZ.Infrastructure.Security;

using System.Security.Cryptography;
using System.Text;
using IGBZ.Application.Abstractions;

/// <summary>
/// رمزنگاری AES-GCM (سند بخش ۱۶): nonce ۱۲ بایت + tag ۱۶ بایت + متن رمزشده.
/// فرمت ذخیره: enc:{base64(nonce + tag + ciphertext)}
/// کلید از تنظیمات (Security:EncryptionKey — base64 ۳۲ بایت) خوانده می‌شود؛ هرگز Hardcode.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private const string Prefix = "enc:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public EncryptionService(string base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            throw new ArgumentException("کلید رمزنگاری الزامی است.", nameof(base64Key));

        try
        {
            _key = Convert.FromBase64String(base64Key);
        }
        catch (FormatException)
        {
            throw new ArgumentException("کلید رمزنگاری باید Base64 باشد.", nameof(base64Key));
        }

        if (_key.Length != 32)
            throw new ArgumentException("کلید رمزنگاری باید ۳۲ بایت (AES-256) باشد.", nameof(base64Key));
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            return plaintext;

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var combined = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, combined, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, combined, NonceSize + TagSize, cipherBytes.Length);

        return Prefix + Convert.ToBase64String(combined);
    }

    public string? Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
            return null;

        // سازگاری با دادهٔ قدیمی/توسعه (بدون رمز) — در Production همه‌چیز enc: دارد
        if (!ciphertext.StartsWith(Prefix, StringComparison.Ordinal))
            return ciphertext;

        try
        {
            var combined = Convert.FromBase64String(ciphertext[Prefix.Length..]);
            if (combined.Length < NonceSize + TagSize + 1)
                return null;

            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var cipherBytes = new byte[combined.Length - NonceSize - TagSize];

            Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(combined, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(combined, NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

            var plainBytes = new byte[cipherBytes.Length];
            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return null; // کلید اشتباه/دادهٔ خراب — رمزگشایی ناموفق
        }
    }
}
