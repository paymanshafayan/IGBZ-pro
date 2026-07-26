using IGBZ.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace IGBZ.Infrastructure.Tenancy;

public sealed class PlatformOptions
{
    public const string SectionName = "Platform";
    public string RootDomain { get; init; } = "localhost";
}

public sealed class OptionsPlatformDomainProvider(IOptions<PlatformOptions> options) : IPlatformDomainProvider
{
    public string RootDomain => options.Value.RootDomain;
}
