namespace IGBZ.Domain.Shared;

public sealed record Address(
    string FullName,
    string PhoneNumber,
    string Province,
    string City,
    string StreetLine,
    string? PostalCode = null,
    string? NationalCode = null);
