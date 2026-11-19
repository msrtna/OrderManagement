using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrderManagement.Application.Abstractions.Identity;

namespace OrderManagement.Infrastructure.Identity;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;
        }
    }
}