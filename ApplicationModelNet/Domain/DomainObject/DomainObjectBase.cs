using Promatis.Net.Domain.Interface;

namespace Promatis.Net.Domain;

/// <summary>
/// Универсальный базовый класс с поддержкой любого типа ключа
/// </summary>
/// <typeparam name="TKey">Тип ключа</typeparam>
public abstract class DomainObjectBase<TKey> : IDomainObjectHasKey<TKey>
{
    public TKey Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}