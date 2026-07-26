using IGBZ.Domain.Shared;

namespace IGBZ.Domain.Orders;

public sealed class OrderCustomerSnapshot
{
    private OrderCustomerSnapshot()
    {
    }

    public OrderCustomerSnapshot(string? customerId, string fullName, string phoneNumber, string? email)
    {
        CustomerId = customerId;
        FullName = Guard.AgainstEmpty(fullName, nameof(fullName));
        PhoneNumber = Guard.AgainstEmpty(phoneNumber, nameof(phoneNumber));
        Email = email?.Trim().ToLowerInvariant();
    }

    public string? CustomerId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Email { get; private set; }
}
