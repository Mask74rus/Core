namespace Promatis.Net.Data;

/// <summary>
/// Модель фиксации изменений (транспортный объект), представляющая снимок состояния 
/// доменной сущности в момент её добавления, модификации или удаления.
/// </summary>
internal class ChangeEntryModel
{
    /// <summary>
    /// Изменяемый экземпляр доменного объекта (сущности).
    /// Приводится к конкретному типу внутри триггеров.
    /// </summary>
    public object Entity { get; init; } = null!;

    /// <summary>
    /// Текущий статус изменения сущности в системе (Added, Modified, Deleted, SoftDeleted).
    /// </summary>
    public EntityStateChangeEnum State { get; init; }

    /// <summary>
    /// Коллекция точечных изменений свойств (полей) сущности.
    /// Заполняется дельтой значений "Старое -> Новое". Для новых сущностей содержит все поля.
    /// </summary>
    public List<PropertyChangeInfo> Changes { get; init; } = [];

    /// <summary>
    /// Идентификатор (имя или логин) пользователя, совершившего данное действие.
    /// </summary>
    public string? ChangedBy { get; init; }

    /// <summary>
    /// Дата и время фиксации изменения в формате UTC.
    /// </summary>
    public DateTime ChangedAt { get; init; }
}