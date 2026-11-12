namespace OrderManagement.Application.Features.Authentication.Register;

public sealed record RegisterResponse(
    string UserId,
    string FirstName,
    string LastName,
    string Email);
