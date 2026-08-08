namespace IGBZ.Application.Auth;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Customers;
using Microsoft.Extensions.Caching.Memory;

public class AuthService : IAuthService
{
    // قفل تلاش ناموفق ورود (سند بخش ۱۶): ۵ تلاش ناموفق → ۱۵ دقیقه قفل
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly ITenantScopedRepository<Customer> _customerRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IMemoryCache _memoryCache;

    public AuthService(
        ITenantScopedRepository<Customer> customerRepository,
        IJwtTokenService jwtTokenService,
        IMemoryCache memoryCache)
    {
        _customerRepository = customerRepository;
        _jwtTokenService = jwtTokenService;
        _memoryCache = memoryCache;
    }

    public async Task<AuthResult> RegisterAsync(RegisterCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
            return new AuthResult { Success = false, ErrorMessage = "شناسهٔ تننت الزامی است." };

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return new AuthResult { Success = false, ErrorMessage = "ایمیل و رمز عبور الزامی است." };

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _customerRepository.FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
        if (existing != null)
            return new AuthResult { Success = false, ErrorMessage = "کاربری با این ایمیل قبلاً ثبت شده است." };

        var customer = new Customer
        {
            TenantId = request.TenantId,
            Email = email,
            Phone = request.Phone?.Trim(),
            PasswordHash = PasswordHasher.Hash(request.Password),
            IsTenantOwner = request.IsTenantOwner,
            IsActive = true,
            CreatedOnUtc = DateTime.UtcNow
        };

        await _customerRepository.InsertAsync(customer, cancellationToken);

        var token = _jwtTokenService.GenerateAccessToken(customer.Id, customer.TenantId, customer.IsTenantOwner);
        return new AuthResult { Success = true, Customer = customer, AccessToken = token };
    }

    public async Task<AuthResult> LoginAsync(string tenantId, string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return new AuthResult { Success = false, ErrorMessage = "ایمیل و رمز عبور الزامی است." };

        var lockKey = $"login-fail:{tenantId}:{email.Trim().ToLowerInvariant()}";

        // بررسی قفل (Brute-force protection)
        if (_memoryCache.TryGetValue(lockKey, out int failedAttempts) && failedAttempts >= MaxFailedAttempts)
            return new AuthResult { Success = false, ErrorMessage = "تلاش‌های ناموفق بیش از حد مجاز بود. ۱۵ دقیقه بعد دوباره تلاش کنید." };

        var customer = await _customerRepository.FirstOrDefaultAsync(
            c => c.Email == email.Trim().ToLowerInvariant(), cancellationToken);

        if (customer == null || !customer.IsActive || !PasswordHasher.Verify(password, customer.PasswordHash))
        {
            // ثبت تلاش ناموفق
            _memoryCache.Set(lockKey, failedAttempts + 1, LockoutDuration);
            return new AuthResult { Success = false, ErrorMessage = "ایمیل یا رمز عبور نادرست است." };
        }

        // موفق → پاک‌کردن شمارندهٔ شکست
        _memoryCache.Remove(lockKey);

        customer.MarkLoggedIn();
        await _customerRepository.UpdateAsync(customer, cancellationToken);

        var token = _jwtTokenService.GenerateAccessToken(customer.Id, customer.TenantId, customer.IsTenantOwner);
        return new AuthResult { Success = true, Customer = customer, AccessToken = token };
    }
}
