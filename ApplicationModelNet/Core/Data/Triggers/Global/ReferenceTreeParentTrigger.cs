using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

/// <summary>
/// Универсальный инфраструктурный триггер для проверки целостности иерархических связей
/// и сквозной защиты от циклических зависимостей перед сохранением в СУБД.
/// </summary>
public class ReferenceTreeParentTrigger : IBeforeSaveTrigger<IDomainObjectHasKey<Guid>>
{
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
            // Сначала ищем родительский узел среди тех, кто прямо сейчас находится в памяти (ChangeTracker).
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
                // Если в оперативной памяти объекта нет, делаем точечный и безопасный запрос к СУБД
                var dbParent = await args.Context.FindAsync(treeEntity.GetType(), parentId.Value) as ReferenceTreeBase;

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
            // Начинаем подъем от нового родителя вверх к корню дерева
            Guid? currentCheckId = parentId;
            var visitedIds = new HashSet<Guid>(); // Страховка от бесконечного зацикливания самого алгоритма

            while (currentCheckId.HasValue && currentCheckId.Value != Guid.Empty)
            {
                // Если на пути вверх мы встретили Id текущей сущности — обнаружена петля
                if (currentCheckId == treeEntity.Id)
                {
                    args.Cancel = true;
                    args.ErrorMessage = "Циклическая зависимость: нельзя переместить родительский узел внутрь собственного дочернего поддерева.";
                    return;
                }

                // Страховка от уже существующих битых данных в БД
                if (!visitedIds.Add(currentCheckId.Value))
                    break;

                // Ищем элемент цепочки сначала в ChangeTracker (вдруг родителя тоже параллельно модифицируют)
                ReferenceTreeBase? nextLocalElement = args.Context.ChangeTracker.Entries<ReferenceTreeBase>()
                    .FirstOrDefault(e => e.Entity.Id == currentCheckId.Value)?.Entity;

                if (nextLocalElement != null)
                {
                    currentCheckId = nextLocalElement.ParentId;
                }
                else
                {
                    Guid? id = currentCheckId;
                    currentCheckId = await args.Context.Set<ReferenceTreeBase>()
                        .AsNoTracking()
                        .IgnoreQueryFilters() // Видим узлы, даже если они мягко удалены
                        .Where(x => x.Id == id.Value)
                        .Select(x => x.ParentId)
                        .FirstOrDefaultAsync();
                }
            }
        }
    }
}