namespace Promatis.Net.Domain.Interface;

/// <summary>
/// Интерфейс для мягкого удаления объектов
/// </summary>
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }

    string? DeletedBy { get; set; }
}