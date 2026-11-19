
namespace OrderManagement.Application.Features.UserAddresses.CreateAddress;

public sealed record CreateAddressRequest(
    string Title,
    string RecipientName,
    string PhoneNumber,
    string Province,
    string City,
    string AddressLine,
    string PostalCode,
    bool IsDefault);
