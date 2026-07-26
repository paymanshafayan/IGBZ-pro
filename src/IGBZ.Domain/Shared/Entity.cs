namespace IGBZ.Domain.Shared;

public abstract class Entity
{
    protected Entity()
    {
    }

    protected Entity(DateTimeOffset now)
    {
        Id = Guid.NewGuid().ToString("N");
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public string Id { get; protected set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;

    public void Touch(DateTimeOffset now) => UpdatedAtUtc = now;
}
