using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.UserAddresses.GetAddresses;

public sealed class GetAddressesHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAddressesHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<GetAddressesResponse?> HandleAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var addresses = await _context.UserAddresses
            .AsNoTracking()
            .Where(address => address.UserId == userId)
            .OrderByDescending(address => address.IsDefault)
            .ThenBy(address => address.Id)
            .Select(address => new AddressItemResponse(
                address.Id,
                address.Title,
                address.RecipientName,
                address.PhoneNumber,
                address.Province,
                address.City,
                address.AddressLine,
                address.PostalCode,
                address.IsDefault))
            .ToListAsync(cancellationToken);

        return new GetAddressesResponse(addresses);
    }
}