
namespace OrderManagement.Application.Features.UserAddresses.UpdateAddress;

public sealed record UpdateAddressRequest(
    string Title,
    string RecipientName,
    string PhoneNumber,
    string Province,
    string City,
    string AddressLine,
    string PostalCode);
