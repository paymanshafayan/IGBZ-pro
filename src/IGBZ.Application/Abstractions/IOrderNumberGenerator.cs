namespace IGBZ.Application.Abstractions;

public interface IOrderNumberGenerator
{
    string Generate(string tenantId, DateTimeOffset now);
}
