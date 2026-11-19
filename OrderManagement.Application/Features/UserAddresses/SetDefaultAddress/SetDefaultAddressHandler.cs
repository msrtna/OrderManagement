
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.UserAddresses.SetDefaultAddress;

public sealed class SetDefaultAddressHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetDefaultAddressHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<SetDefaultAddressResponse?> HandleAsync(
        long addressId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var address = await _context.UserAddresses
            .FirstOrDefaultAsync(
                item => item.Id == addressId &&
                        item.UserId == userId,
                cancellationToken);

        if (address is null)
        {
            return null;
        }

        if (address.IsDefault)
        {
            return new SetDefaultAddressResponse(
                address.Id,
                true);
        }

        var existingDefaultAddress = await _context.UserAddresses
            .FirstOrDefaultAsync(
                item => item.UserId == userId &&
                        item.IsDefault,
                cancellationToken);

        if (existingDefaultAddress is not null)
        {
            existingDefaultAddress.IsDefault = false;
        }

        address.IsDefault = true;

        await _context.SaveChangesAsync(cancellationToken);

        return new SetDefaultAddressResponse(
            address.Id,
            address.IsDefault);
    }
}
