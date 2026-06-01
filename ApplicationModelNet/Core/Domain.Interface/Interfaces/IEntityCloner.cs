namespace Promatis.Net.Domain.Interface;

/// <summary>
/// Интерфейс платформенного движка изолированного клонирования доменных сущностей.
/// </summary>
public interface IEntityCloner
{
    T CloneEntity<T>(T entity) where T : class;
}