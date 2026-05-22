using Promatis.Net.UI.Components.BaseWorkspace;

namespace Promatis.Net.UI.Components.BaseToolbarWorkspacePage;

/// <summary>
/// Специализированный контекст действий для рабочих областей, имеющих тулбар управления.
/// Автоматизирует геометрию командных панелей и доступность кнопок CRUD.
/// </summary>
/// <typeparam name="TEntity">Тип доменного объекта (модели), отображаемого на форме.</typeparam>
public class ToolbarActionContext<TEntity> : WorkspaceActionContext, IHasToolbar where TEntity : class
{
    // Реализация интерфейса контракта тулбаров IHasToolbar
    public ToolbarPosition Position { get; set; } = ToolbarPosition.Top;
    public bool IsToolbarVisible { get; set; } = true;

    private TEntity? _selectedData;

    /// <summary>
    /// Текущий выделенный объект (строка в таблице, узел в дереве или элемент на мнемосхеме).
    /// При изменении автоматически пересчитывает доступность всех базовых кнопок тулбара.
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
                NotifyUpdate(); // Форсирует мгновенную перерисовку Blazor-каркаса
            }
        }
    }

    // =========================================================================
    // 1. СТАТИЧЕСКАЯ КОНФИГУРАЦИЯ (Управляется со страницы, например, по правам доступа)
    // =========================================================================

    public bool IsCreateVisible { get; set; } = true;
    public bool IsEditVisible { get; set; } = true;
    public bool IsDeleteVisible { get; set; } = true;

    /// <summary>
    /// Кнопка создания по умолчанию активна всегда. Страница может затушить её принудительно.
    /// </summary>
    public virtual bool IsCreateEnabled { get; set; } = true;

    // =========================================================================
    // 2. ДИНАМИЧЕСКИЙ РАСЧЕТ СТЭЙТА (Строго Read-Only, завязано на фокус данных)
    // =========================================================================

    /// <summary>
    /// Кнопка активна, если объект выбран И внутренние бизнес-правила разрешают редактирование.
    /// </summary>
    public virtual bool IsEditEnabled => SelectedData != null && CanEditNode(SelectedData);

    /// <summary>
    /// Кнопка активна, если объект выбран И внутренние бизнес-правила разрешают удаление.
    /// </summary>
    public virtual bool IsDeleteEnabled => SelectedData != null && CanDeleteNode(SelectedData);

    /// <summary>
    /// Коллекция кастомных действий (экспорт, печать и т.д.).
    /// </summary>
    public List<ToolbarCustomAction> CustomActions { get; } = [];

    public ToolbarActionContext()
    {
        WorkspaceHeight = "100%";
        TopZoneHeight = "auto";
        BottomZoneHeight = "auto";
        LeftZoneWidth = "220px";
        RightZoneWidth = "220px";
    }

    /// <summary>
    /// Безопасное изменение доступности кастомной кнопки по её Id из кода бэкенда страницы
    /// </summary>
    public void SetActionEnabled(string actionId, bool isEnabled)
    {
        var action = CustomActions.FirstOrDefault(a => a.Id == actionId);
        if (action != null && action.IsEnabled != isEnabled)
        {
            action.IsEnabled = isEnabled;
            NotifyUpdate();
        }
    }

    // =========================================================================
    // ДОБАВЛЕНО: РЕАКТИВНАЯ ФИЛЬТРАЦИЯ ТИПОВ ДАННЫХ ПРИ ТРАНЗАКЦИЯХ СУБД
    // =========================================================================

    /// <summary>
    /// Перехватывает сигнал коммита от верхнего уровня каркаса.
    /// Проверяет полиморфизм типов доменной модели и реактивно пинает вложенный контент.
    /// </summary>
    public override void HandleGlobalEntityCommit(object state, object entity)
    {
        base.HandleGlobalEntityCommit(state, entity);

        // ИСПРАВЛЕНО: Безопасно извлекаем чистый базовый тип, даже если EF Core обернул его в Прокси
        Type entityType = entity.GetType();
        if (entityType.BaseType != null && entityType.Namespace == "Castle.Proxies")
        {
            entityType = entityType.BaseType; // Спускаемся к реальному доменному типу
        }

        // Сравниваем чистые типы метаданных
        if (typeof(TEntity).IsAssignableFrom(entityType))
        {
            string stateStr = state.ToString() ?? string.Empty;

            if (stateStr == "Deleted" || stateStr == "SoftDeleted")
            {
                if (SelectedData != null && ReferenceEquals(SelectedData, entity))
                {
                    SelectedData = null;
                }
            }

            // Пингаем UI
            RequestRefresh();
        }
    }

    // =========================================================================
    // 3. ДОМЕННЫЕ ПРЕДИКАТЫ-ХУКИ (Для переопределения бизнес-логики в наследниках)
    // =========================================================================

    /// <summary>
    /// Проверяет, разрешено ли редактировать конкретный выделенный объект. По умолчанию - всегда да.
    /// </summary>
    protected virtual bool CanEditNode(TEntity node) => true;

    /// <summary>
    /// Проверяет, разрешено ли удалять конкретный выделенный объект. По умолчанию - всегда да.
    /// </summary>
    protected virtual bool CanDeleteNode(TEntity node) => true;

    /// <summary>
    /// Хук жизненного цикла платформы для выполнения дополнительных расчетов при смене фокуса.
    /// </summary>
    protected virtual void RecalculateButtonStates()
    {
    }
}