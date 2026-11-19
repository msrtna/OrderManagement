using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Abstractions.Identity;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Features.UserAddresses.CreateAddress;

public sealed class CreateAddressHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CreateAddressRequest> _validator;

    public CreateAddressHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IValidator<CreateAddressRequest> validator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _validator = validator;
    }

    public async Task<CreateAddressResult> HandleAsync(
        CreateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(
            request,
            cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(error => new CreateAddressError(
                    error.ErrorCode,
                    error.ErrorMessage))
                .Distinct()
                .ToList();

            return new CreateAddressResult(
                false,
                null,
                errors);
        }

        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Failure(
                "UserNotAuthenticated",
                "The current user is not authenticated.");
        }

        var address = new UserAddress
        {
            UserId = userId,
            Title = request.Title.Trim(),
            RecipientName = request.RecipientName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Province = request.Province.Trim(),
            City = request.City.Trim(),
            AddressLine = request.AddressLine.Trim(),
            PostalCode = request.PostalCode.Trim(),
            IsDefault = request.IsDefault
        };

        if (address.IsDefault)
        {
            var existingDefaultAddress = await _context.UserAddresses
                .FirstOrDefaultAsync(
                    item => item.UserId == userId && item.IsDefault,
                    cancellationToken);

            if (existingDefaultAddress is not null)
            {
                existingDefaultAddress.IsDefault = false;
            }
        }

        var hasAnyAddress = await _context.UserAddresses
            .AnyAsync(
                item => item.UserId == userId,
                cancellationToken);

        if (!hasAnyAddress)
        {
            address.IsDefault = true;
        }

        await _context.UserAddresses.AddAsync(
            address,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var response = new CreateAddressResponse(
            address.Id,
            address.Title,
            address.RecipientName,
            address.PhoneNumber,
            address.Province,
            address.City,
            address.AddressLine,
            address.PostalCode,
            address.IsDefault);

        return new CreateAddressResult(
            true,
            response,
            Array.Empty<CreateAddressError>());
    }

    private static CreateAddressResult Failure(
        string errorCode,
        string errorMessage)
    {
        return new CreateAddressResult(
            false,
            null,
            new[]
            {
                new CreateAddressError(
                    errorCode,
                    errorMessage)
            });
    }
}
