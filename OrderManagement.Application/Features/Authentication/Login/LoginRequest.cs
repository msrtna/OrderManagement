namespace OrderManagement.Application.Features.Authentication.Login;

public sealed record LoginRequest(
    string Email,
    string Password);