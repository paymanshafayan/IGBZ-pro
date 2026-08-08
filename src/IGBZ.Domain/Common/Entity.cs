namespace IGBZ.Domain.Common;

/// <summary>
/// پایهٔ همهٔ موجودیت‌ها — Id از نوع string (در لایهٔ زیرساخت با ObjectId پر می‌شود).
/// </summary>
public abstract class Entity
{
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOnUtc { get; set; }
}
