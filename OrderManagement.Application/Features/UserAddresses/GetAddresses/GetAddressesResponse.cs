namespace OrderManagement.Application.Features.UserAddresses.GetAddresses;

public sealed record GetAddressesResponse(
    IReadOnlyCollection<AddressItemResponse> Addresses);

public sealed record AddressItemResponse(
    long AddressId,
    string Title,
    string RecipientName,
    string PhoneNumber,
    string Province,
    string City,
    string AddressLine,
    string PostalCode,
    bool IsDefault);