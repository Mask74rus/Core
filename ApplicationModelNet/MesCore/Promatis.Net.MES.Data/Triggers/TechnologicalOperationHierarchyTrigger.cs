using Microsoft.EntityFrameworkCore;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.MES.Domain.Interface;
using System.Reflection;

namespace Promatis.Net.MES.Data;

/// <summary>
/// Триггер контроля иерархии
/// </summary>
public class TechnologicalOperationHierarchyTrigger : IBeforeSaveTrigger<ITechnologicalOperation>
{
    public async Task HandleAsync(EntityCancelEventArgs<ITechnologicalOperation> args)
    {
        Type entityType = args.Entity.GetType();

        // 1. Через рефлексию извлекаем свойство ParentId.
        // Если его нет (хотя по архитектуре оно обязано быть), то это не дерево, мы выходим.
        PropertyInfo? parentIdProp = entityType.GetProperty("ParentId");
        if (parentIdProp == null) return;

        Guid? parentId = (Guid?)parentIdProp.GetValue(args.Entity);

        // 2. Если у операции задан родительский узел — запускаем проверку бизнес-правила
        if (parentId.HasValue && parentId.Value != Guid.Empty)
        {
            // Вычисляем корневой CLR-тип для корректного сканирования DbSet в EF Core
            var entityTypeInModel = args.Context.Model.FindEntityType(entityType);
            Type rootClrType = entityTypeInModel?.GetRootType()?.ClrType ?? entityType;

            // Извлекаем DbSet для базового CLR-типа операции
            IQueryable<object> dbSet = ((IQueryable)args.Context.GetType()
                .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
                .MakeGenericMethod(rootClrType)
                .Invoke(args.Context, null)!).Cast<object>();

            // Выкачиваем родителя из базы данных, используя универсальный механизм EF.Property
            var parentData = await dbSet
                .Where(x => EF.Property<Guid>(x, "Id") == parentId.Value)
                .Select(x => new
                {
                    Id = EF.Property<Guid>(x, "Id"),
                    IsLeaf = EF.Property<bool>(x, "IsLeaf"),
                    Name = EF.Property<string>(x, "Name")
                })
                .FirstOrDefaultAsync();

            // 3. БИЗНЕС-ПРАВИЛО ПЛАТФОРМЫ: Проверяем флаг "листа" у родительского узла
            if (parentData != null && parentData.IsLeaf)
            {
                args.Cancel = true;
                args.ErrorMessage = $"Критическая ошибка структуры: Нельзя добавить дочерний элемент в конечную операцию '{parentData.Name}' (IsLeaf = true).";
            }
        }
    }
}