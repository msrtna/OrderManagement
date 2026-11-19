
namespace OrderManagement.Application.Features.UserAddresses.DeleteAddress;

public sealed record DeleteAddressResponse(
    long AddressId,
    bool Succeeded);
