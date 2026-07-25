namespace IGBZ.Application.Abstractions;

public sealed record StockReservationResult(bool Succeeded, string? ReservationId, string? FailureReason)
{
    public static StockReservationResult Success(string reservationId) => new(true, reservationId, null);
    public static StockReservationResult Failure(string reason) => new(false, null, reason);
}

public interface IInventoryService
{
    Task EnsureInventoryItemAsync(string productId, string variantId, int initialStock, CancellationToken cancellationToken = default);
    Task<StockReservationResult> TryReserveAsync(string productId, string variantId, int quantity, string reason, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task CommitReservationAsync(string reservationId, CancellationToken cancellationToken = default);
    Task ReleaseReservationAsync(string reservationId, CancellationToken cancellationToken = default);
}
