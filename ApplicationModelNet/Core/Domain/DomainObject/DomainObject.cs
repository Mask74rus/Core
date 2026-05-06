namespace Promatis.Net.Domain;

/// <summary>
/// Основной класс (GUID по умолчанию)
/// </summary>
public abstract class DomainObject : DomainObjectBase<Guid>
{
    protected DomainObject()
    {
        Id = Guid.NewGuid();
    }
}