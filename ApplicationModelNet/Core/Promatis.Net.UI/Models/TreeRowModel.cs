using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI;

public class TreeRowModel<TEntity> where TEntity : class, IDomainObject
{
    // Околонулевой маппинг: прямая ссылка на домен
    public TEntity DomainEntity { get; }

    public Guid Id { get; private set; }
    public Guid? ParentId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }

    public TreeRowModel(TEntity entity)
    {
        DomainEntity = entity ?? throw new ArgumentNullException(nameof(entity));

        // 1. Безопасное извлечение Id через Pattern Matching доменного предка
        if (entity is DomainObject baseDomain)
        {
            Id = baseDomain.Id;
        }

        // 2. Извлечение ParentId из древовидного предка справочников
        if (entity is ReferenceTreeBase treeRef)
        {
            ParentId = treeRef.ParentId;
            Name = treeRef.Name;
            Code = treeRef.Code;
        }
    }
}