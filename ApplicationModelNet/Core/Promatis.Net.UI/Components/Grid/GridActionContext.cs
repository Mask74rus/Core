using Promatis.Net.UI.Components.Toolbar;

namespace Promatis.Net.UI.Components.Grid;

/// <summary>
/// Базовый табличный контекст управления. 
/// Объединяет в себе метаданные тулбара и брокера данных для плоских списков и реестров.
/// </summary>
/// <typeparam name="TEntity">Тип доменной сущности таблицы</typeparam>
public class GridActionContext<TEntity> : ToolbarActionContext<TEntity> where TEntity : class
{
    /// <summary>
    /// Удобный алиас для работы с выделенной строкой в прикладных таблицах.
    /// </summary>
    public TEntity? SelectedItem
    {
        get => SelectedData;
        set => SelectedData = value;
    }

    /// <summary>
    /// Инкапсулированный платформенный брокер данных для текущего грида.
    /// Через него таблица будет асинхронно запрашивать строки у бэкенд-служб.
    /// </summary>
    public UiDataBroker<TEntity> DataBroker { get; } = new();

    public GridActionContext() : base()
    {
        // Фиксируем стандартное верхнее расположение командного тулбара для всех таблиц
        Position = ToolbarPosition.Top;
    }

    /// <summary>
    /// Переопределяем хук изменения фокуса, чтобы форсировать мгновенное 
    /// реактивное обновление кнопок на тулбаре при штатном клике по строке.
    /// </summary>
    protected override void RecalculateButtonStates()
    {
        base.RecalculateButtonStates();
        NotifyUpdate(); // Пингаем UiToolbar на перерисовку стейта кнопок
    }
}