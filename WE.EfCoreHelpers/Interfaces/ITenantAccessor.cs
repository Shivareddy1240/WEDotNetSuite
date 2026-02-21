namespace WE.EfCoreHelpers;

public interface ITenantAccessor
{
    Guid TenantId { get; }
}