namespace IGBZ.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
