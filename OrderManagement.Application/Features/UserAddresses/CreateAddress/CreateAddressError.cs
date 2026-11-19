namespace OrderManagement.Application.Features.UserAddresses.CreateAddress;

public sealed record CreateAddressError(
    string ErrorCode,
    string ErrorMessage);