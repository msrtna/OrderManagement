namespace OrderManagement.Application.Features.UserAddresses.CreateAddress;

public sealed record CreateAddressResult(
    bool Succeeded,
    CreateAddressResponse? Response,
    IReadOnlyCollection<CreateAddressError> Errors);