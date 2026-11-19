
namespace OrderManagement.Application.Features.UserAddresses.SetDefaultAddress;

public sealed record SetDefaultAddressResponse(
    long AddressId,
    bool IsDefault);
