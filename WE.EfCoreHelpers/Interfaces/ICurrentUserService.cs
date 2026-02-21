namespace WE.EfCoreHelpers;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    // Add roles, tenantId, etc. if needed later
}