using IdentityService.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace IdentityService.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirst("MemberId")?.Value;

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}
