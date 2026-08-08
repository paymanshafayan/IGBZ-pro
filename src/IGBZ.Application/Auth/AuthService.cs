namespace IGBZ.Application.Auth;

using IGBZ.Application.Abstractions;
using IGBZ.Domain.Customers;

public class AuthService : IAuthService
{
    private readonly ITenantScopedRepository<Customer> _customerRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(ITenantScopedRepository<Customer> customerRepository, IJwtTokenService jwtTokenService)
    {
        _customerRepository = customerRepository;
        _jwtTokenService = jwtTokenService;
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

        var customer = await _customerRepository.FirstOrDefaultAsync(
            c => c.Email == email.Trim().ToLowerInvariant(), cancellationToken);

        if (customer == null || !customer.IsActive || !PasswordHasher.Verify(password, customer.PasswordHash))
            return new AuthResult { Success = false, ErrorMessage = "ایمیل یا رمز عبور نادرست است." };

        customer.MarkLoggedIn();
        await _customerRepository.UpdateAsync(customer, cancellationToken);

        var token = _jwtTokenService.GenerateAccessToken(customer.Id, customer.TenantId, customer.IsTenantOwner);
        return new AuthResult { Success = true, Customer = customer, AccessToken = token };
    }
}
