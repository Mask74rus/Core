using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain.Interface;
using Promatis.Net.UI.Components.Toolbar;

namespace Promatis.Net.UI.Components.Tree;

/// <summary>
/// Базовый иерархический (древовидный) контекст управления платформы.
/// Полностью инкапсулирует состояние базовых кнопок и фокуса для MudTreeView.
/// </summary>
/// <typeparam name="TEntity">Тип древовидной сущности, реализующей ITreeNode</typeparam>
public abstract class TreeActionContext<TEntity> : ToolbarActionContext<TEntity>
        where TEntity : class, ITreeNode<TEntity>, IDomainObjectHasKey<Guid>
{
    // Статическая настройка видимости дочерней кнопки
    public bool IsCreateChildVisible { get; set; } = true;

    // Динамический расчет доступности кнопки добавления подузла
    public virtual bool IsCreateChildEnabled => SelectedData != null;

    protected IDialogService DialogService => ScopedProvider.GetRequiredService<IDialogService>();
    protected IValidator<TEntity> GlobalValidator => ScopedProvider.GetRequiredService<IValidator<TEntity>>();

    /// <summary>
    /// Платформенный брокер данных, адаптированный под иерархические структуры.
    /// </summary>
    public UiDataBroker<TEntity> DataBroker { get; } = new();

    protected TreeActionContext() : base()
    {
        // ИСПРАВЛЕНО: Переносим командный тулбар наверх (горизонтальная панель)
        Position = ToolbarPosition.Top;
        TopZoneHeight = "auto";

        // Схлопываем левую зону, так как тулбар ушел наверх
        IsLeftZoneCollapsed = true;
        LeftZoneWidth = "0px";

        // По умолчанию дерево инициализируется в режиме работы с оперативной памятью
        DataBroker.ConfigureInMemoryMode();
    }

    /// <summary>
    /// Обязательный для реализации метод прогрева ОЗУ-кэша. 
    /// Вызывается автоматически компонентом TreePage при первичной отрисовке.
    /// </summary>
    public abstract Task InitializeInMemoryTreeAsync();

    /// <summary>
    /// Автоматический перехват коммитов СУБД на уровне иерархического ядра.
    /// Обеспечивает сквозную real-time синхронизацию связей веток в ОЗУ за 0 мс.
    /// </summary>
    public override void HandleGlobalEntityCommit(object? state, object? entity)
    {
        if (entity == null) return;

        Type entityType = entity.GetType();
        if (entityType.BaseType != null && entityType.Namespace == "Castle.Proxies")
        {
            entityType = entityType.BaseType;
        }

        // Если транзакция из бэкенда затронула наш древовидный тип данных
        if (typeof(TEntity).IsAssignableFrom(entityType))
        {
            string stateStr = state?.ToString() ?? string.Empty;

            // Пиняем ОЗУ-движок брокера пересчитать связи массива в памяти
            DataBroker.ApplyIncrementalOzuDelta(stateStr, (TEntity)entity);

            // Вызываем базовые правила сброса фокуса (если выделенная ветка была удалена)
            base.HandleGlobalEntityCommit(state, entity);

            // Пингаем компонент TreePage полностью перерисовать дерево на экране
            RequestRefresh();
        }
    }

    protected override void RecalculateButtonStates()
    {
        base.RecalculateButtonStates();
        NotifyUpdate(); // Реактивно обновляем активность кнопки "Добавить подузел" при смене фокуса
    }
}