namespace IGBZ.Domain.Customers;

/// <summary>
/// مشتری/کاربر پلتفرم — به یک تننت (فروشگاه) تعلق دارد؛ مالک تننت با
/// <see cref="IsTenantOwner"/> مشخص می‌شود.
/// </summary>
public class Customer : Entity, ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    /// <summary>Hash رمز عبور (PBKDF2) — رمز خام هرگز ذخیره نمی‌شود.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsTenantOwner { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginUtc { get; set; }

    public void MarkLoggedIn()
    {
        LastLoginUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
