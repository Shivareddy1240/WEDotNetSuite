using Microsoft.AspNetCore.Http;

namespace WE.EfCoreHelpers;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
                          ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name
                          ?? "Anonymous";

    public string? UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name
                            ?? "Anonymous";
}