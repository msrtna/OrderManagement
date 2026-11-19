
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;

namespace OrderManagement.Application.Features.UserAddresses.UpdateAddress;

public sealed class UpdateAddressHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<UpdateAddressRequest> _validator;

    public UpdateAddressHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IValidator<UpdateAddressRequest> validator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _validator = validator;
    }

    public async Task<UpdateAddressResult> HandleAsync(
        long addressId,
        UpdateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            return Failure(
                "ValidationFailed",
                string.Join(
                    "; ",
                    validationResult.Errors
                        .Select(error => error.ErrorMessage)
                        .Distinct()));
        }

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Failure(
                "UserNotAuthenticated",
                "The current user is not authenticated.");
        }

        var address = await _context.UserAddresses
            .FirstOrDefaultAsync(
                item => item.Id == addressId &&
                        item.UserId == userId,
                cancellationToken);

        if (address is null)
        {
            return Failure(
                "AddressNotFound",
                "The requested address was not found.");
        }

        address.Title = request.Title.Trim();
        address.RecipientName = request.RecipientName.Trim();
        address.PhoneNumber = request.PhoneNumber.Trim();
        address.Province = request.Province.Trim();
        address.City = request.City.Trim();
        address.AddressLine = request.AddressLine.Trim();
        address.PostalCode = request.PostalCode.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        var response = new UpdateAddressResponse(
            address.Id,
            address.Title,
            address.RecipientName,
            address.PhoneNumber,
            address.Province,
            address.City,
            address.AddressLine,
            address.PostalCode,
            address.IsDefault);

        return new UpdateAddressResult(
            true,
            response,
            null,
            null);
    }

    private static UpdateAddressResult Failure(
        string errorCode,
        string errorMessage)
    {
        return new UpdateAddressResult(
            false,
            null,
            errorCode,
            errorMessage);
    }
}
