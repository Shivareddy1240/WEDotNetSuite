using Microsoft.EntityFrameworkCore;

namespace WE.EfCoreHelpers;

public static class SoftDeleteExtensions
{
    /// <summary>
    /// Undeletes (restores) a soft-deleted entity
    /// </summary>
    public static void Undelete<TEntity>(this DbContext context, TEntity entity)
        where TEntity : class, ISoftDeletable
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entity);

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        context.Entry(entity).State = EntityState.Modified;
    }

    /// <summary>
    /// Includes soft-deleted entities in the query (bypasses filter)
    /// </summary>
    public static IQueryable<TEntity> IncludeDeleted<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class, ISoftDeletable
    {
        return query.IgnoreQueryFilters();
    }
}