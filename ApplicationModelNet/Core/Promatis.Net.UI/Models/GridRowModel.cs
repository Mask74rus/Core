using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using System.Reflection;

namespace Promatis.Net.UI;


public class GridRowModel<TEntity> : IGridReferenceViewModel
    where TEntity : class, IDomainObject // Исходный доменный интерфейс с CreatedAt
{
    // Околонулевой маппинг: оригинальный доменный объект всегда доступен в разметке
    public TEntity DomainEntity { get; }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }

    public GridRowModel(TEntity entity)
    {
        DomainEntity = entity ?? throw new ArgumentNullException(nameof(entity));

        // 1. Строго типизированное извлечение Id через доменный базовый класс
        if (entity is DomainObject baseDomainObj)
        {
            Id = baseDomainObj.Id;
        }
        else
        {
            // Фолбэк-защита: если объект реализует IDomainObject, но не наследует DomainObject,
            // мы можем извлечь Id через типизированный интерфейс (если вы решите добавить Id в IDomainObject)
            Id = Guid.Empty;
        }

        // 2. Умное и безопасное извлечение Name и Code для справочников
        if (entity is ReferenceBase reference)
        {
            Name = reference.Name;
            Code = reference.Code;
        }
        else
        {
            // Фолбэк для объектов без Name (например, AuditLog).
            // Используем явное приведение к конкретному типу для исключения dynamic.
            if (entity is Promatis.Net.Domain.AuditLog auditLog)
            {
                Name = auditLog.EntityName;
            }
        }
    }
}