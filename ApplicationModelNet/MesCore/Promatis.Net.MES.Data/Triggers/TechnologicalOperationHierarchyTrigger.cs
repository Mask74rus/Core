using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Data;

/// <summary>
/// Триггер контроля иерархии
/// </summary>
public class TechnologicalOperationHierarchyTrigger : IBeforeSaveTrigger<ITechnologicalOperation>
{
    public async Task HandleAsync(EntityCancelEventArgs<ITechnologicalOperation> args)
    {
        // 1. Приводим к ReferenceTreeBase, чтобы получить доступ к физическому полю ParentId в БД.
        // Это безопасно, так как все наши операции наследуются от ReferenceTreeBase.
        if (args.Entity is not ReferenceTreeBase op) return;

        // 2. Если у операции задан родительский узел
        if (op.ParentId.HasValue && op.ParentId != Guid.Empty)
        {
            // Используем маркерный интерфейс для сканирования таблицы в EF Core.
            // Благодаря наследованию сущностей в EF Core, Set<ITechnologicalOperation>() 
            // автоматически обратится к нужной физической таблице.
            var parent = await args.Context.Set<ITechnologicalOperation>()
                .Cast<ReferenceTreeBase>() // Приводим тип внутри LINQ-выражения для доступа к IsLeaf
                .Select(x => new { x.Id, IsLeaf = EF.Property<bool>(x, "IsLeaf"), Name = EF.Property<string>(x, "Name") })
                .FirstOrDefaultAsync(x => x.Id == op.ParentId);

            // 3. БИЗНЕС-ПРАВИЛО: Если родитель является "листом" (IsLeaf == true),
            // отменяем сохранение и передаем текст ошибки в интерцептор
            if (parent != null && parent.IsLeaf)
            {
                args.Cancel = true;
                args.ErrorMessage = $"Критическая ошибка структуры: Нельзя добавить дочерний элемент в конечную операцию '{parent.Name}' (IsLeaf = true).";
            }
        }
    }
}