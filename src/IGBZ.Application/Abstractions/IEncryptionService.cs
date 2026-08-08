namespace IGBZ.Application.Abstractions;

/// <summary>
/// رمزنگاری کلیدهای حساس (سند بخش ۱۶): اعتبارنامه‌های API همیشه با AES-GCM رمز می‌شوند.
/// <see cref="Decrypt"/> برای سازگاری با دادهٔ قدیمی/توسعه، اگر مقدار پیشوند «enc:» نداشت،
/// همان مقدار خام را برمی‌گرداند (در Production همهٔ مقادیر رمز شده‌اند).
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string? Decrypt(string ciphertext);
}
