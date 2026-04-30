using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Data;

public class ReferenceTreeParentTrigger : IBeforeSaveTrigger<IDomainObjectHasKey<Guid>>
{
    // Кэшируем MethodInfo для метода Set<T>()
    private static readonly MethodInfo SetMethod = typeof(DbContext)
        .GetMethods()
        .First(m => m.Name == nameof(DbContext.Set) &&
                    m.IsGenericMethod &&
                    m.GetParameters().Length == 0);

    public async Task HandleAsync(EntityCancelEventArgs<IDomainObjectHasKey<Guid>> args)
    {
        Type entityType = args.Entity.GetType();
        PropertyInfo? parentIdProp = entityType.GetProperty("ParentId");

        if (parentIdProp == null) return;

        var parentId = (Guid?)parentIdProp.GetValue(args.Entity);

        // Защита от самоцитирования
        if (parentId.HasValue && parentId == args.Entity.Id)
        {
            args.Cancel = true;
            args.ErrorMessage = "Объект не может быть родителем самому себе.";
            return;
        }

        if (parentId.HasValue && parentId.Value != Guid.Empty)
        {
            // Используем наш закэшированный SetMethod для получения IQueryable
            MethodInfo genericSetMethod = SetMethod.MakeGenericMethod(entityType);
            var dbSet = genericSetMethod.Invoke(args.Context, null) as IQueryable;

            if (dbSet != null)
            {
                // Приводим к интерфейсам через Cast. 
                // В EF Core можно цепочкой приводить к разным интерфейсам для доступа к свойствам
                var parentData = await dbSet
                    .Cast<IDomainObjectHasKey<Guid>>()
                    .Where(x => x.Id == parentId.Value)
                    .Cast<ISoftDeletable>() // Приводим к ISoftDeletable для проверки DeletedAt
                    .Select(x => new { x.DeletedAt })
                    .FirstOrDefaultAsync();

                if (parentData == null)
                {
                    args.Cancel = true;
                    args.ErrorMessage = $"Указанный родительский объект (ID: {parentId}) не найден.";
                    return;
                }

                if (parentData.DeletedAt != null)
                {
                    args.Cancel = true;
                    args.ErrorMessage = "Нельзя назначить родителем удаленный объект.";
                }
            }
        }
    }
}
