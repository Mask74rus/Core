namespace Promatis.Net.Domain.Interface;

public interface IDomainObjectHasKey<TKey> : IDomainObject
{
    TKey Id { get; set; }
}