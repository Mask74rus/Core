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
    // Кэшируем шаблоны методов на уровне класса для максимальной производительности
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
        // Проверяем, относится ли сохраняемая сущность к древовидным структурам Promatis
        if (args.Entity is not ReferenceTreeBase treeEntity)
            return;

        Guid? parentId = treeEntity.ParentId;

        // 1. Защита от прямого самоцитирования (узел указывает сам на себя)
        if (parentId.HasValue && parentId == treeEntity.Id)
        {
            args.Cancel = true;
            args.ErrorMessage = "Объект не может быть родителем самому себе.";
            return;
        }

        if (parentId.HasValue && parentId.Value != Guid.Empty)
        {
            // Сначала ищем родительский узел среди тех, кто прямо сейчас находится в памяти (ChangeTracker)
            ReferenceTreeBase? localParent = args.Context.ChangeTracker.Entries<ReferenceTreeBase>()
                .FirstOrDefault(e => e.Entity.Id == parentId.Value)?.Entity;

            bool exists;
            bool isDeleted;

            if (localParent != null)
            {
                exists = true;
                isDeleted = localParent.DeletedAt != null;
            }
            else
            {
                // Вычисляем реальный корневой тип модели (например, UnitBase)
                IEntityType? entityTypeInModel = args.Context.Model.FindEntityType(treeEntity.GetType());
                Type rootClrType = entityTypeInModel?.GetRootType()?.ClrType ?? treeEntity.GetType();

                // ИСПРАВЛЕНО ДЛЯ .NET 10: Собираем строго типизированную цепочку вызовов через Reflection
                // Шаг А: Вызов context.Set<rootClrType>()
                object? rawSet = DbContextSetMethod.MakeGenericMethod(rootClrType).Invoke(args.Context, null);

                // Шаг Б: Вызов EntityFrameworkQueryableExtensions.AsNoTracking<rootClrType>(query)
                object? noTrackingQuery = AsNoTrackingMethod.MakeGenericMethod(rootClrType).Invoke(null, [rawSet]);

                // Шаг В: Вызов EntityFrameworkQueryableExtensions.IgnoreQueryFilters<rootClrType>(query)
                object? ignoreFiltersQuery = IgnoreQueryFiltersMethod.MakeGenericMethod(rootClrType).Invoke(null,
                    [noTrackingQuery]);

                // Преобразуем результат в IQueryable<object> через стандартный не-дженерик интерфейс IQueryable
                IQueryable<object> query = ((IQueryable)ignoreFiltersQuery!).Cast<object>();

                // Ищем объект по динамическому свойству "Id" СУБД
                object? dbParentObj = await query.FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == parentId.Value);
                var dbParent = dbParentObj as ReferenceTreeBase;

                exists = dbParent != null;
                isDeleted = dbParent?.DeletedAt != null;
            }

            // 2. Проверка физического существования родительского узла
            if (!exists)
            {
                args.Cancel = true;
                args.ErrorMessage = $"Указанный родительский объект (ID: {parentId}) не найден.";
                return;
            }

            // 3. Проверка статуса мягкого удаления (Soft Delete) родителя
            if (isDeleted)
            {
                args.Cancel = true;
                args.ErrorMessage = "Нельзя назначить родителем удаленный объект.";
                return;
            }

            // 4. ГЛУБОКАЯ ЗАЩИТА ОТ ЦИКЛОВ (Поиск петель любой глубины: A -> B -> C -> A)
            Guid? currentCheckId = parentId;
            var visitedIds = new HashSet<Guid>(); // Страховка от бесконечного зацикливания

            while (currentCheckId.HasValue && currentCheckId.Value != Guid.Empty)
            {
                if (currentCheckId == treeEntity.Id)
                {
                    args.Cancel = true;
                    args.ErrorMessage = "Циклическая зависимость: нельзя переместить родительский узел внутрь собственного дочернего поддерева.";
                    return;
                }

                if (!visitedIds.Add(currentCheckId.Value))
                    break;

                // Ищем элемент цепочки сначала в ChangeTracker
                ReferenceTreeBase? nextLocalElement = args.Context.ChangeTracker.Entries<ReferenceTreeBase>()
                    .FirstOrDefault(e => e.Entity.Id == currentCheckId.Value)?.Entity;

                if (nextLocalElement != null)
                {
                    currentCheckId = nextLocalElement.ParentId;
                }
                else
                {
                    Guid? id = currentCheckId;

                    IEntityType? loopEntityTypeInModel = args.Context.Model.FindEntityType(treeEntity.GetType());
                    Type loopRootClrType = loopEntityTypeInModel?.GetRootType()?.ClrType ?? treeEntity.GetType();

                    // Аналогичная Reflection-цепочка вызовов внутри цикла защиты от петель
                    object? loopRawSet = DbContextSetMethod.MakeGenericMethod(loopRootClrType).Invoke(args.Context, null);
                    object? loopNoTrackingQuery = AsNoTrackingMethod.MakeGenericMethod(loopRootClrType).Invoke(null, [loopRawSet]);
                    object? loopIgnoreFiltersQuery = IgnoreQueryFiltersMethod.MakeGenericMethod(loopRootClrType).Invoke(null, [loopNoTrackingQuery]);

                    IQueryable<object> loopQuery = ((IQueryable)loopIgnoreFiltersQuery!).Cast<object>();

                    object? parentNodeObj = await loopQuery.FirstOrDefaultAsync(x => EF.Property<Guid>(x, "Id") == id.Value);

                    currentCheckId = (parentNodeObj as ReferenceTreeBase)?.ParentId;
                }
            }
        }
    }
}