using IGBZ.Application.Abstractions;

namespace IGBZ.Application.Common;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
