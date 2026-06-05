using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Абстрактное ядро работы со стейтом конкретного экземпляра сущности в памяти.
/// Монопольно отвечает за хранение выделенной записи (SelectedData) и изолированного черновика (DraftData).
/// Передает 3 generic-параметра наверх в DataContext для сквозной синхронизации Брокера данных.
/// </summary>
public abstract class EntityContext<TEntity, TKey, TQueryState, TResultData>(
    IServiceProvider serviceProvider,
    bool isInMemoryMode) : DataContext<TEntity, TQueryState, TResultData>(serviceProvider, isInMemoryMode)
    where TEntity : class, new()
    where TKey : notnull
{
    private TEntity? _selectedData;
    private TEntity? _draftData;

    /// <summary>
    /// Текущая выделенная пользователем запись в UI (в гриде или дереве).
    /// При изменении генерирует единый импульс NotifyContextUpdated() для реактивного обновления всего экрана.
    /// </summary>
    public TEntity? SelectedData
    {
        get => _selectedData;
        set
        {
            if (_selectedData != value)
            {
                _selectedData = value;
                NotifyContextUpdated(); // Пинает единый event обновления для тулбара и страницы
            }
        }
    }

    /// <summary>
    /// Изолированный черновик (глубокий клон) сущности для прямой привязки к полям ввода в UI.
    /// Наличие объекта в этом слоте автоматически сигнализирует базовой странице ядра о фазе мутации (CRUD).
    /// </summary>
    public TEntity? DraftData
    {
        get => _draftData;
        set
        {
            if (_draftData != value)
            {
                _draftData = value;
                NotifyContextUpdated(); // Пинает единое событие обновления для открытия/закрытия окон ввода
            }
        }
    }

    /// <summary>
    /// Служебный хелпер ядра для извлечения строго типизированного первичного ключа Id из сущности.
    /// Предоставляет универсальный инструмент для работы репозиториев СУБД PostgreSQL на нижних слоях.
    /// </summary>
    public TKey GetEntityKey(TEntity entity)
    {
        if (entity is IDomainObjectHasKey<TKey> hasKeyEntity)
        {
            return hasKeyEntity.Id;
        }

        throw new InvalidOperationException(
            $"Критический сбой домена: Сущность '{typeof(TEntity).Name}' не реализует обязательный интерфейс 'IDomainObjectHasKey<{typeof(TKey).Name}>'.");
    }
}