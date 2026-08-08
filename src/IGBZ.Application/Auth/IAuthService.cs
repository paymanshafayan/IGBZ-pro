namespace IGBZ.Application.Auth;

using IGBZ.Domain.Customers;

public class RegisterCustomerRequest
{
    public string TenantId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public bool IsTenantOwner { get; init; }
}

public class AuthResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Customer? Customer { get; init; }
    public string? AccessToken { get; init; }
}

/// <summary>ثبت‌نام و ورود مشتریان + صدور JWT.</summary>
public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> LoginAsync(string tenantId, string email, string password, CancellationToken cancellationToken = default);
}
