
namespace OrderManagement.Application.Features.UserAddresses.UpdateAddress;

public sealed record UpdateAddressResponse(
    long AddressId,
    string Title,
    string RecipientName,
    string PhoneNumber,
    string Province,
    string City,
    string AddressLine,
    string PostalCode,
    bool IsDefault);
