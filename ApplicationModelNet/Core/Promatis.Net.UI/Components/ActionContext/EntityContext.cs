using Microsoft.Extensions.DependencyInjection;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.UI.Components;

/// <summary>
/// Базовый контекст сущности, инкапсулирующий состояние выбранной пользователем строки.
/// Реализует интерфейс IEntityContext для бесшовной связи с кнопками интерфейса.
/// </summary>
/// <typeparam name="TEntity">Класс бизнес-сущности (например, User, AuditLog).</typeparam>
/// <typeparam name="TQueryState">Тип состояния запроса MudBlazor (например, GridState).</typeparam>
/// <typeparam name="TResultData">Тип контейнера результатов (например, GridData).</typeparam>
public abstract class EntityContext<TEntity, TQueryState, TResultData>
    : DataContext<TEntity, TQueryState, TResultData>, IEntityContext
    where TEntity : class, new()
{
    /// <summary>
    /// Текущая выделенная пользователем запись в UI (строго типизированная для прикладного C#-кода страниц).
    /// При изменении значения генерирует единый импульс NotifyContextUpdated() для пересчета доступности кнопок.
    /// </summary>
    public TEntity? SelectedData
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                NotifyContextUpdated(); // Мгновенно оповещает кнопки тулбара и формы о смене селекшена
            }
        }
    }

    /// <summary>
    /// ТИХИЙ КОНСТРУКТОР СЛОЯ СУЩНОСТИ.
    /// Передает serviceProvider наверх в DataContext для извлечения Брокера и Ozu-кэша.
    /// </summary>
    protected EntityContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <summary>
    /// ЯВНАЯ РЕАЛИЗАЦИЯ ИНТЕРФЕЙСА IEntityContext ДЛЯ НЕ-GENERIC КОМПОНЕНТОВ (RenderBase)
    /// Безопасный проброс выбранной строки в виде object? для пассивных визуализаторов кнопок.
    /// </summary>
    object? IEntityContext.SelectedData
    {
        get => SelectedData;
        set => SelectedData = (TEntity?)value; // Абсолютно безопасный downcasting в рамках конкретного экрана
    }
}