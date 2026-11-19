
namespace OrderManagement.Application.Features.UserAddresses.UpdateAddress;

public sealed record UpdateAddressResult(
    bool Succeeded,
    UpdateAddressResponse? Response,
    string? ErrorCode,
    string? ErrorMessage);
