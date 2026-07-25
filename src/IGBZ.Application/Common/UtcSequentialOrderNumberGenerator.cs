using System.Threading;
using IGBZ.Application.Abstractions;

namespace IGBZ.Application.Common;

public sealed class UtcSequentialOrderNumberGenerator : IOrderNumberGenerator
{
    private long _counter;

    public string Generate(string tenantId, DateTimeOffset now)
    {
        var sequence = Interlocked.Increment(ref _counter) % 100000;
        var tenantPart = tenantId.Length <= 6 ? tenantId : tenantId[..6];
        return $"{tenantPart.ToUpperInvariant()}-{now:yyyyMMddHHmmss}-{sequence:00000}";
    }
}
