namespace IGBZ.Domain.Payments;

public enum PaymentStatus
{
    Pending = 0,
    RedirectCreated = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4
}
