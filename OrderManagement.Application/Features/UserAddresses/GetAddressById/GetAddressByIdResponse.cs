
namespace OrderManagement.Application.Features.UserAddresses.GetAddressById;

public sealed record GetAddressByIdResponse(
    long AddressId,
    string Title,
    string RecipientName,
    string PhoneNumber,
    string Province,
    string City,
    string AddressLine,
    string PostalCode,
    bool IsDefault);
