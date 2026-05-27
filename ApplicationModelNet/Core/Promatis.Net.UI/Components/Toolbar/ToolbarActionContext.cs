using Promatis.Net.UI.Components.Workspace;

namespace Promatis.Net.UI.Components.Toolbar;

/// <summary>
/// Глобальный контекст управления командной панелью (тулбаром).
/// Полностью абстрагирован от конкретных способов получения данных и типов визуализаторов.
/// </summary>
public abstract class ToolbarActionContext<TEntity> : WorkspaceActionContext, IHasToolbar, IHasSelectedData<TEntity>
    where TEntity : class
{
    // Реализация контракта IHasToolbar
    public ToolbarPosition Position { get; set; } = ToolbarPosition.Top;
    public bool IsToolbarVisible { get; set; } = true;

    private TEntity? _selectedData;

    /// <summary>
    /// Текущий выделенный объект на холсте (строка грида, ветка дерева, датчик схемы).
    /// При изменении автоматически пересчитывает доступность базовых кнопок и пинает Blazor на перерисовку.
    /// </summary>
    public TEntity? SelectedData
    {
        get => _selectedData;
        set
        {
            if (!EqualityComparer<TEntity>.Default.Equals(_selectedData, value))
            {
                _selectedData = value;
                RecalculateButtonStates();
                NotifyUpdate(); // Форсирует мгновенную перерисовку тулбара и подсветок
            }
        }
    }

    // СТАТИЧЕСКАЯ НАСТРОЙКА ВИДИМОСТИ КНОПОК
    public bool IsCreateVisible { get; set; } = true;
    public bool IsEditVisible { get; set; } = true;
    public bool IsDeleteVisible { get; set; } = true;

    // ДИНАМИЧЕСКИЙ РАСЧЕТ ДОСТУПНОСТИ КНОПОК (Read-Only)

    /// <summary>
    /// Создание по умолчанию доступно всегда (если кнопка видима).
    /// </summary>
    public virtual bool IsCreateEnabled { get; set; } = true;

    /// <summary>
    /// Изменение доступно, если объект выбран И предикат бизнес-логики разрешает операцию.
    /// </summary>
    public virtual bool IsEditEnabled => SelectedData != null && CanEditNode(SelectedData);

    /// <summary>
    /// Удаление доступно, если объект выбран И предикат бизнес-логики разрешает операцию.
    /// </summary>
    public virtual bool IsDeleteEnabled => SelectedData != null && CanDeleteNode(SelectedData);

    /// <summary>
    /// Коллекция расширенных (кастомных) кнопок тулбара (печать, экспорт, импорт).
    /// </summary>
    private readonly List<ToolbarCustomAction> _customActions = [];

    /// <summary>
    /// Безопасная коллекция кастомных действий, доступная тулбару только для чтения.
    /// Исключает случайную порчу или очистку списка кнопок извне.
    /// </summary>
    public IReadOnlyCollection<ToolbarCustomAction> CustomActions => _customActions.AsReadOnly();

    protected ToolbarActionContext()
    {
        // По умолчанию настраиваем компактную геометрию для тулбарных страниц
        WorkspaceHeight = "100%";
        TopZoneHeight = "auto";
        BottomZoneHeight = "auto";
        LeftZoneWidth = "220px";
        RightZoneWidth = "220px";
    }

    /// <summary>
    /// Защищенный метод для безопасного добавления кастомных кнопок из конструкторов прикладных контекстов.
    /// Гарантирует fail-fast поведение и защищает систему от дублирования идентификаторов кнопок.
    /// </summary>
    protected void AddCustomAction(ToolbarCustomAction action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        // Защита от невнимательности разработчика: Id кнопок на одной странице обязаны быть уникальными
        if (_customActions.Any(a => a.Id == action.Id))
        {
            throw new InvalidOperationException(
                $"Кастомная кнопка с идентификатором '{action.Id}' уже зарегистрирована в контексте '{PageTitle}'.");
        }

        _customActions.Add(action);
    }

    /// <summary>
    /// Программное изменение доступности кастомной кнопки по её Id из бизнес-логики контекста.
    /// </summary>
    public void SetActionEnabled(string actionId, bool isEnabled)
    {
        // Ищем внутри закрытого списка
        ToolbarCustomAction? action = _customActions.Find(a => a.Id == actionId);
        if (action != null && action.IsEnabled != isEnabled)
        {
            action.IsEnabled = isEnabled;
            NotifyUpdate(); // Реактивно обновляем UI
        }
    }

    // РЕАКТИВНАЯ ФИЛЬТРАЦИЯ ТРАНЗАКЦИЙ СУБД ПО ТИПУ СУЩНОСТИ

    public override void HandleGlobalEntityCommit(object? state, object? entity)
    {
        base.HandleGlobalEntityCommit(state, entity);

        if (entity == null) return;

        Type entityType = entity.GetType();
        if (entityType.BaseType != null && entityType.Namespace == "Castle.Proxies")
        {
            entityType = entityType.BaseType;
        }

        if (typeof(TEntity).IsAssignableFrom(entityType))
        {
            string stateStr = state?.ToString() ?? string.Empty;

            if (stateStr == "Deleted" || stateStr == "SoftDeleted")
            {
                if (SelectedData != null && ReferenceEquals(SelectedData, entity))
                {
                    SelectedData = null;
                }
            }

            RequestRefresh();
        }
    }

    // ДОМЕННЫЕ ПРЕДИКАТЫ-ХУКИ (Переопределяются в прикладных контекстах)
    protected virtual bool CanEditNode(TEntity node) => true;
    protected virtual bool CanDeleteNode(TEntity node) => true;

    /// <summary>
    /// Внутренний хук жизненного цикла для проведения дополнительных расчетов при смене фокуса.
    /// </summary>
    protected virtual void RecalculateButtonStates()
    {
    }
}
