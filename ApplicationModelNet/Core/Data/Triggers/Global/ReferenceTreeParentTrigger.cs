using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Reflection;

namespace Promatis.Net.Data;

/// <summary>
/// Универсальный инфраструктурный триггер для проверки целостности иерархических связей
/// и сквозной защиты от циклических зависимостей перед сохранением в СУБД.
/// </summary>
public class ReferenceTreeParentTrigger : IBeforeSaveTrigger<IDomainObjectHasKey<Guid>>
{
    private static readonly MethodInfo DbContextSetMethod = typeof(DbContext)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .First(m => m.Name == nameof(DbContext.Set) && m.IsGenericMethod && m.GetParameters().Length == 0);

    private static readonly MethodInfo AsNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking) && m.IsGenericMethod);

    private static readonly MethodInfo IgnoreQueryFiltersMethod = typeof(EntityFrameworkQueryableExtensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.IgnoreQueryFilters) && m.IsGenericMethod);

    public async Task HandleAsync(EntityCancelEventArgs<IDomainObjectHasKey<Guid>> args)
    {
        // ПРОСТОЕ РЕШЕНИЕ: Через рефлексию проверяем, есть ли у объекта свойство ParentId.
        // Если свойства нет — этот объект не дерево, мы просто выходим!
        PropertyInfo? parentIdProp = args.Entity.GetType().GetProperty("ParentId");
        if (parentIdProp == null) return;

        // Читаем значения свойств динамически
        Guid entityId = args.Entity.Id;
        Guid? parentId = (Guid?)parentIdProp.GetValue(args.Entity);

        PropertyInfo? deletedAtProp = args.Entity.GetType().GetProperty("DeletedAt");

        // 1. Защита от прямого самоцитирования
        if (parentId.HasValue && parentId == entityId)
        {
            args.Cancel = true;
            args.ErrorMessage = "Объект не может быть родителем самому себе.";
            return;
        }

        if (parentId.HasValue && parentId.Value != Guid.Empty)
        {
            // Ищем родителя в ChangeTracker среди любых объектов, у которых совпадает Id
            object? localParent = args.Context.ChangeTracker.Entries()
                .FirstOrDefault(e => ((IDomainObjectHasKey<Guid>)e.Entity).Id == parentId.Value)?.Entity;

            bool exists;
            bool isDeleted;

            if (localParent != null)
            {
                exists = true;
                var localDeletedAt = (DateTime?)deletedAtProp?.GetValue(localParent);
                isDeleted = localDeletedAt != null;
            }
            else
            {
                // Ваша оригинальная рефлексия для вычисления корня и Query в EF Core
                IEntityType? entityTypeInModel = args.Context.Model.FindEntityType(args.Entity.GetType());
                Type rootClrType = entityTypeInModel?.GetRootType()?.ClrType ?? args.Entity.GetType();

                object? rawSet = DbContextSetMethod.MakeGenericMethod(rootClrType).Invoke(args.Context, null);
                object? noTrackingQuery = AsNoTrackingMethod.MakeGenericMethod(rootClrType).Invoke(null, [rawSet]);
                object? ignoreFiltersQuery = IgnoreQueryFiltersMethod.MakeGenericMethod(rootClrType).Invoke(null, [noTrackingQuery]);

                IQueryable<object> query = ((IQueryable)ignoreFiltersQuery!).Cast<object>();

                // Ищем объект по динамическому свойству "Id" СУБД
                object? dbParent = await query.FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == parentId.Value);

                exists = dbParent != null;
                var dbDeletedAt = dbParent != null ? (DateTime?)deletedAtProp?.GetValue(dbParent) : null;
                isDeleted = dbDeletedAt != null;
            }

            // 2. Проверка физического существования родительского узла
            if (!exists)
            {
                args.Cancel = true;
                args.ErrorMessage = $"Указанный родительский объект (ID: {parentId}) не найден.";
                return;
            }

            // 3. Проверка статуса мягкого удаления
            if (isDeleted)
            {
                args.Cancel = true;
                args.ErrorMessage = "Нельзя назначить родителем удаленный объект.";
                return;
            }

            // 4. ГЛУБОКАЯ ЗАЩИТА ОТ ЦИКЛОВ (A -> B -> C -> A)
            Guid? currentCheckId = parentId;
            var visitedIds = new HashSet<Guid>();

            while (currentCheckId.HasValue && currentCheckId.Value != Guid.Empty)
            {
                if (currentCheckId == entityId)
                {
                    args.Cancel = true;
                    args.ErrorMessage = "Циклическая зависимость: нельзя переместить родительский узел внутрь собственного дочернего поддерева.";
                    return;
                }

                if (!visitedIds.Add(currentCheckId.Value))
                    break;

                // Ищем элемент цепочки сначала в ChangeTracker
                object? nextLocalElement = args.Context.ChangeTracker.Entries()
                    .FirstOrDefault(e => ((IDomainObjectHasKey<Guid>)e.Entity).Id == currentCheckId.Value)?.Entity;

                if (nextLocalElement != null)
                {
                    currentCheckId = (Guid?)parentIdProp.GetValue(nextLocalElement);
                }
                else
                {
                    Guid? id = currentCheckId;

                    IEntityType? loopEntityTypeInModel = args.Context.Model.FindEntityType(args.Entity.GetType());
                    Type loopRootClrType = loopEntityTypeInModel?.GetRootType()?.ClrType ?? args.Entity.GetType();

                    object? loopRawSet = DbContextSetMethod.MakeGenericMethod(loopRootClrType).Invoke(args.Context, null);
                    object? loopNoTrackingQuery = AsNoTrackingMethod.MakeGenericMethod(loopRootClrType).Invoke(null, [loopRawSet]);
                    object? loopIgnoreFiltersQuery = IgnoreQueryFiltersMethod.MakeGenericMethod(loopRootClrType).Invoke(null, [loopNoTrackingQuery]);

                    IQueryable<object> loopQuery = ((IQueryable)loopIgnoreFiltersQuery!).Cast<object>();

                    object? parentNodeObj = await loopQuery.FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id.Value);

                    currentCheckId = parentNodeObj != null ? (Guid?)parentIdProp.GetValue(parentNodeObj) : null;
                }
            }
        }
    }
}