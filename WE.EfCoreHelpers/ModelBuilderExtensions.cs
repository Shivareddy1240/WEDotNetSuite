using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using WE.EfCoreHelpers;
using WE.EfCoreHelpers.Interfaces;  // Assuming your interfaces are here

namespace WE.EfCoreHelpers;

public static class ModelBuilderExtensions
{
    private static readonly Regex SnakeCaseRegex = new Regex("(?<!^)([A-Z][a-z0-9]|(?<=[a-z0-9])[A-Z])", RegexOptions.Compiled);

    /// <summary>
    /// Applies snake_case naming to tables/columns, UTC DateTime handling, and auto query filters for soft-delete and multi-tenancy.
    /// Call this in your DbContext OnModelCreating method.
    /// </summary>
    public static ModelBuilder ApplyWEConventions(this ModelBuilder modelBuilder, ITenantAccessor? tenantAccessor = null)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Snake case table name
            var tableName = entityType.GetTableName();
            if (tableName != null)
            {
                entityType.SetTableName(ToSnakeCase(tableName));
            }

            // Snake case column names
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (columnName != null)
                {
                    property.SetColumnName(ToSnakeCase(columnName));
                }
            }

            // UTC DateTime converter for all DateTime properties
            foreach (var property in entityType.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                property.SetValueConverter(
                    new ValueConverter<DateTime, DateTime>(
                        v => v.ToUniversalTime(),
                        v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
            }

            var clrType = entityType.ClrType;

            // Auto apply soft-delete filter
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var filter = BuildTypedFilter<ISoftDeletable>(clrType, e => !EF.Property<bool>(e, nameof(ISoftDeletable.IsDeleted)));
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
            }

            // Auto apply multi-tenant filter (only if tenantAccessor provided)
            if (tenantAccessor != null && typeof(IMultiTenantEntity).IsAssignableFrom(clrType))
            {
                var filter = BuildTypedFilter<IMultiTenantEntity>(clrType,
                    e => EF.Property<Guid>(e, nameof(IMultiTenantEntity.TenantId)) == tenantAccessor.TenantId);

                modelBuilder.Entity(clrType).HasQueryFilter(filter);
            }
        }

        return modelBuilder;
    }

    /// <summary>
    /// Builds a strongly-typed lambda expression for HasQueryFilter from a generic interface filter.
    /// This solves "delegate type could not be inferred" error.
    /// </summary>
    private static LambdaExpression BuildTypedFilter<TInterface>(Type concreteType, Expression<Func<TInterface, bool>> interfaceFilter)
    {
        // Create parameter of concrete type
        var parameter = Expression.Parameter(concreteType, "e");

        // Replace interface parameter with concrete one
        var body = ReplacingExpressionVisitor.Replace(
            interfaceFilter.Parameters[0],
            parameter,
            interfaceFilter.Body);

        return Expression.Lambda(body, parameter);
    }

    private static string ToSnakeCase(string name)
    {
        return SnakeCaseRegex.Replace(name, "_$1").ToLowerInvariant();
    }
}