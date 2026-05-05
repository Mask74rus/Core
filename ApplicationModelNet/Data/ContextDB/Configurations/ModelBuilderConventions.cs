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

            // Обработка всех свойств сущности (лимиты для строк)
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                // Устанавливаем 255 по умолчанию, если не задано иное
                if (property.ClrType == typeof(string))
                    if (property.GetMaxLength() == null) property.SetMaxLength(255);
            }

            // Авто-ключи для Guid
            if (typeof(IDomainObjectHasKey<Guid>).IsAssignableFrom(type))
            {
                modelBuilder.Entity(type).Property("Id").ValueGeneratedNever();
            }

            // АВТО-ФИЛЬТР и АВТО-ИНДЕКС для всех SoftDelete объектов
            if (typeof(ISoftDeletable).IsAssignableFrom(type))
            {
                // А вот ФИЛЬТР ставим ТОЛЬКО на корень
                // Проверяем: если у типа нет базового типа ИЛИ базовый тип не реализует ISoftDeletable
                IMutableEntityType? baseType = entityType.BaseType;
                if (baseType == null || !typeof(ISoftDeletable).IsAssignableFrom(baseType.ClrType))
                {
                    modelBuilder.SetSoftDeleteFilter(type);
                }
            }
        }
    }

    private static void SetSoftDeleteFilter(this ModelBuilder modelBuilder, Type entityType)
    {
        // Вместо сложной рефлексии используем встроенный механизм EF для динамических фильтров
        // если это возможно, либо проверяем, что entityType действительно класс
        if (entityType.IsClass && !entityType.IsAbstract)
            modelBuilder.Entity(entityType).HasQueryFilter(GenerateFilter(entityType));
    }

    // Вспомогательный метод для создания выражения e => e.DeletedAt == null
    private static LambdaExpression GenerateFilter(Type type)
    {
        ParameterExpression parameter = Expression.Parameter(type, "e");
        MemberExpression property = Expression.Property(parameter, "DeletedAt");
        ConstantExpression nullConstant = Expression.Constant(null, typeof(DateTime?));
        BinaryExpression body = Expression.Equal(property, nullConstant);
        return Expression.Lambda(body, parameter);
    }
}