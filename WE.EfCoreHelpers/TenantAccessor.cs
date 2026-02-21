using Microsoft.AspNetCore.Http;

namespace WE.EfCoreHelpers;

public class TenantAccessor : ITenantAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var tenantIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value
                           ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();

            return Guid.TryParse(tenantIdStr, out var id) ? id : Guid.Empty;
        }
    }
}