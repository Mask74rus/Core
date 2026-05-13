using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

/// <summary>
/// Универсальный инфраструктурный триггер для проверки целостности иерархических связей
/// перед непосредственным сохранением данных в СУБД.
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
            // Это гарантирует стабильную работу юнит-тестов и транзакционных цепочек.
            var localParent = args.Context.ChangeTracker.Entries<ReferenceTreeBase>()
                .FirstOrDefault(e => e.Entity.Id == parentId.Value)?.Entity;

            bool exists;
            bool isDeleted;

            if (localParent != null)
            {
                exists = true;
                isDeleted = localParent.DeletedAt != null; // ReferenceTreeBase наследует ReferenceBase (SoftDelete)
            }
            else
            {
                // Если в оперативной памяти объекта нет, делаем точечный, безопасный запрос к БД.
                // Передаем исходный тип через GetType(), что исключает сбои при работе с EF-прокси.
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
            }
        }
    }
}