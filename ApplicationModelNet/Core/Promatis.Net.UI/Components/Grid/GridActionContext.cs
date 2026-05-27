using Promatis.Net.UI.Components.Toolbar;

namespace Promatis.Net.UI.Components.Grid;

/// <summary>
/// Базовый табличный контекст управления платформы.
/// Автоматически обеспечивает высокопроизводительные ОЗУ-мутации при любых транзакциях в СУБД.
/// </summary>
public abstract class GridActionContext<TEntity> : ToolbarActionContext<TEntity> where TEntity : class
{
    public TEntity? SelectedItem
    {
        get => SelectedData;
        set => SelectedData = value;
    }

    /// <summary>
    /// Универсальный брокер данных текущей таблицы.
    /// </summary>
    public UiDataBroker<TEntity> DataBroker { get; } = new();

    public GridActionContext() : base()
    {
        Position = ToolbarPosition.Top;
    }

    // =========================================================================
    // ПЛАТФОРМЕННЫЙ АВТОМАТИЧЕСКИЙ ДВИЖОК ОЗУ-МУТАЦИЙ (ПОДНЯТ НАВЕРХ)
    // =========================================================================

    /// <summary>
    /// Глобальный перехватчик коммитов СУБД на уровне табличного ядра.
    /// Полностью освобождает прикладных разработчиков от ручного написания логики обновлений.
    /// </summary>
    public override void HandleGlobalEntityCommit(object? state, object? entity)
    {
        if (entity == null) return;

        Type entityType = entity.GetType();

        // Срезаем динамические прокси Castle/EF Core
        if (entityType.BaseType != null && entityType.Namespace == "Castle.Proxies")
        {
            entityType = entityType.BaseType;
        }

        // 1. Если транзакция из базы данных затронула именно тип данных нашей таблицы TEntity
        if (typeof(TEntity).IsAssignableFrom(entityType))
        {
            string stateStr = state?.ToString() ?? string.Empty;

            // 2. Автоматически пинаем ОЗУ-движок брокера применить дельту за 0 мс
            DataBroker.ApplyIncrementalOzuDelta(stateStr, (TEntity)entity);

            // 3. Вызываем базовые правила сброса фокуса ToolbarActionContext
            base.HandleGlobalEntityCommit(state, entity);

            // 4. Автоматически пинаем GridPage перерисовать HTML-строки из обновленного кэша
            RequestRefresh();
        }
    }

    protected override void RecalculateButtonStates()
    {
        base.RecalculateButtonStates();
        NotifyUpdate();
    }
}