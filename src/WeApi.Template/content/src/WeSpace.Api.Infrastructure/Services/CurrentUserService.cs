using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WeSpace.Api.Application.Interfaces;

namespace WeSpace.Api.Infrastructure.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

#if useIntId
    public int? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var subClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user?.FindFirst("sub")?.Value;

            return int.TryParse(subClaim, out var id) ? id : null;
        }
    }
#elif useLongId
    public long? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var subClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user?.FindFirst("sub")?.Value;

            return long.TryParse(subClaim, out var id) ? id : null;
        }
    }
#else
    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var subClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? user?.FindFirst("sub")?.Value;

            return Guid.TryParse(subClaim, out var id) ? id : null;
        }
    }
#endif

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
