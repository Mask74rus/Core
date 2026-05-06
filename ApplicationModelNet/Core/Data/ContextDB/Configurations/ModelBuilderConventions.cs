using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

public static class ModelBuilderConventions
{
    public static void ApplyGlobalConventions(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            Type type = entityType.ClrType;

            // 1. Лимиты для строк (255 по умолчанию)
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string))
                {
                    if (property.GetMaxLength() == null)
                        property.SetMaxLength(255);
                }
            }

            // 2. Авто-ключи для Guid (ValueGeneratedNever, так как Id создается в DomainObject)
            if (typeof(IDomainObjectHasKey<Guid>).IsAssignableFrom(type))
            {
                modelBuilder.Entity(type).Property("Id").ValueGeneratedNever();
            }

            // 3. АВТО-ФИЛЬТР и АВТО-ИНДЕКС для SoftDelete объектов
            if (typeof(ISoftDeletable).IsAssignableFrom(type))
            {
                IMutableEntityType? baseType = entityType.BaseType;

                // Ставим фильтр только на корень иерархии (включая абстрактный UnitBase)
                if (baseType == null || !typeof(ISoftDeletable).IsAssignableFrom(baseType.ClrType))
                {
                    // Установка QueryFilter
                    modelBuilder.SetSoftDeleteFilter(type);

                    // Установка индекса на DeletedAt для ускорения запросов
                    modelBuilder.Entity(type).HasIndex("DeletedAt");
                }
            }
        }
    }

    private static void SetSoftDeleteFilter(this ModelBuilder modelBuilder, Type entityType)
    {
        // Для TPT/TPH важно ставить фильтр на корень, даже если он абстрактный.
        // Условие IsClass достаточно.
        if (entityType.IsClass)
        {
            modelBuilder.Entity(entityType).HasQueryFilter(GenerateFilter(entityType));
        }
    }

    private static LambdaExpression GenerateFilter(Type type)
    {
        ParameterExpression parameter = Expression.Parameter(type, "e");
        // Используем Property для доступа к DeletedAt через интерфейс или свойство
        MemberExpression property = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
        ConstantExpression nullConstant = Expression.Constant(null, typeof(DateTime?));
        BinaryExpression body = Expression.Equal(property, nullConstant);

        return Expression.Lambda(body, parameter);
    }
}