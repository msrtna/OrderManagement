namespace OrderManagement.Application.Abstractions.Identity;

public interface IIdentityService
{
    Task<RegisterResult> RegisterCustomerAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record IdentityError(
    string Code,
    string Description);

public sealed record RegisterResult(
    bool Succeeded,
    string? UserId,
    IReadOnlyList<IdentityError> Errors);
