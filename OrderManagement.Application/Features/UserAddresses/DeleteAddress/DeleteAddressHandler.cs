
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.UserAddresses.DeleteAddress;

public sealed class DeleteAddressHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAddressHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<DeleteAddressResponse?> HandleAsync(
        long addressId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
            return null;

        var address = await _context.UserAddresses
            .FirstOrDefaultAsync(
                item => item.Id == addressId && item.UserId == userId,
                cancellationToken);

        if (address is null)
            return null;

        var wasDefault = address.IsDefault;

        _context.UserAddresses.Remove(address);

        if (wasDefault)
        {
            var replacementAddress = await _context.UserAddresses
                .Where(item => item.UserId == userId && item.Id != addressId)
                .OrderBy(item => item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (replacementAddress is not null)
                replacementAddress.IsDefault = true;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new DeleteAddressResponse(addressId, true);
    }
}
