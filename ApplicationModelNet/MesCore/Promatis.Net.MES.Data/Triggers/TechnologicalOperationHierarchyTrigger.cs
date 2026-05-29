using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.Data;

/// <summary>
/// Абстрактный базовый триггер иерархии технологических операций.
/// Защищает структуру от некорректного добавления подузлов и контролирует статусы IsLeaf.
/// </summary>
public abstract class TechnologicalOperationHierarchyTrigger<T, TOLink, TPLink> : IBeforeSaveTrigger<T>
    where T : TechnologicalOperationBase<T, TOLink, TPLink>
    where TOLink : class
    where TPLink : class
{
    public async Task HandleAsync(EntityCancelEventArgs<T> args)
    {
        T operation = args.Entity;

        // Если операция корневая — пропускаем проверку
        if (!operation.ParentId.HasValue || operation.ParentId == Guid.Empty) return;

        // Извлекаем из СУБД статус IsLeaf и Name родительской операции
        var parentInfo = await args.Context.Set<T>()
            .Where(x => x.Id == operation.ParentId)
            .Select(x => new { x.IsLeaf, x.Name })
            .FirstOrDefaultAsync();

        if (parentInfo != null)
        {
            // Используем наш доменный движок для проверки правил вложенности.
            // Передаем статус IsLeaf родителя, вытащенный из базы.
            if (parentInfo.IsLeaf)
            {
                args.Cancel = true;
                args.ErrorMessage = $"Нарушение иерархии MES: Технологическая операция '{operation.Name}' " +
                                    $"не может быть вложена в '{parentInfo.Name}', так как она является терминальным узлом (Листом).";
                return;
            }
        }

        // Дополнительная защита на изменение: если операция становится Листом, 
        // проверяем в ChangeTracker или БД, нет ли у нее дочерних элементов
        if (operation.IsLeaf)
        {
            bool hasChildrenInDb = await args.Context.Set<T>()
                .AnyAsync(x => x.ParentId == operation.Id);

            if (!TechnologicalOperationHierarchyEngine.CanChangeLeafStatus(hasChildrenInDb, operation.IsLeaf))
            {
                args.Cancel = true;
                args.ErrorMessage = $"Нарушение целостности данных: Невозможно сделать операцию '{operation.Name}' терминальной (Листом), " +
                                    $"так как внутри нее уже содержатся другие технологические операции.";
            }
        }
    }
}