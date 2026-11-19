
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.UserAddresses.GetAddressById;

public sealed class GetAddressByIdHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAddressByIdHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetAddressByIdResponse?> HandleAsync(
        long addressId,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return await _context.UserAddresses
            .AsNoTracking()
            .Where(address =>
                address.Id == addressId &&
                address.UserId == userId)
            .Select(address => new GetAddressByIdResponse(
                address.Id,
                address.Title,
                address.RecipientName,
                address.PhoneNumber,
                address.Province,
                address.City,
                address.AddressLine,
                address.PostalCode,
                address.IsDefault))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
