using MudBlazor;

namespace Promatis.Net.UI.Components.Workspace;

/// <summary>
/// Базовый контекст любой рабочей области (вкладки) в системе.
/// Общий знаменатель для мнемосхем, справочников, дашбордов и графиков.
/// </summary>
public abstract class WorkspaceActionContext : IWorkspaceActionContext
{
    /// <summary>
    /// Провайдер служб текущей Scoped-сессии пользователя. 
    /// Заполняется автоматически визуальным холстом при старте страницы.
    /// </summary>
    public IServiceProvider ScopedProvider { get; set; } = null!;

    /// <summary>
    /// Уникальный идентификатор рабочей области в рантайме.
    /// </summary>
    public Guid WorkspaceId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Текстовый заголовок страницы (отображается на тулбаре или вкладке).
    /// </summary>
    public string PageTitle { get; set; } = "Рабочая область";

    /// <summary>
    /// Системное имя модуля (Core, Mes, MesMDM), к которому относится экран.
    /// </summary>
    public string ModuleName { get; init; } = string.Empty;

    // УПРАВЛЕНИЕ ВИЗУАЛЬНЫМ СТИЛЕМ ПОДЛОЖКИ
    public int PaperElevation { get; set; } = 1;
    public string PaperClass { get; set; } = "pa-4 d-flex flex-column flex-grow-1 w-100 h-100";

    // УПРАВЛЕНИЕ ГЕОМЕТРИЕЙ КАРКАСА ХОЛСТА (Связь с WorkspacePage)
    public string WorkspaceHeight { get; set; } = "100%";
    public string TopZoneHeight { get; set; } = "auto";
    public string BottomZoneHeight { get; set; } = "auto";
    public string LeftZoneWidth { get; set; } = "250px";
    public string RightZoneWidth { get; set; } = "300px";

    public bool IsTopZoneCollapsed { get; set; } = false;
    public bool IsBottomZoneCollapsed { get; set; } = false;
    public bool IsLeftZoneCollapsed { get; set; } = false;
    public bool IsRightZoneCollapsed { get; set; } = false;

    // РЕАКТИВНЫЙ МОСТ ВЗАИМОДЕЙСТВИЯ (Blazor - Контекст)

    /// <summary>
    /// Событие, вызываемое при изменении свойств самого контекста (например, геометрии или заголовка).
    /// Принудительно заставляет Blazor перерисовать элементы.
    /// </summary>
    public Action? OnContextUpdated { get; set; }

    /// <summary>
    /// Триггер мгновенного оповещения UI-элементов о внутреннем изменении стейта контекста.
    /// </summary>
    public void NotifyUpdate() => OnContextUpdated?.Invoke();

    /// <summary>
    /// Глобальное событие, извещающее вложенные визуализаторы (грид, дерево) о необходимости обновить данные.
    /// Срабатывает при командах перезагрузки или по сигналам из СУБД.
    /// </summary>
    public event Action? OnRefreshRequested;

    /// <summary>
    /// Вспомогательный метод для безопасного запуска обновления вложенного контента изнутри контекста.
    /// </summary>
    protected void RequestRefresh() => OnRefreshRequested?.Invoke();

    /// <summary>
    /// Вызывается холстом WorkspacePage при успешном коммите любой сущности в СУБД.
    /// Переопределяется в типизированных наследниках для точечной проверки типов данных.
    /// </summary>
    /// <param name="state">Состояние изменения из перехватчика EF Core (EntityStateChangeEnum)</param>
    /// <param name="entity">Сам доменный объект (чистый или проксированный)</param>
    public virtual void HandleGlobalEntityCommit(object? state, object? entity)
    {
    }

    /// <summary>
    /// Централизованно обеспечивает цветовое единообразие операций во всех модулях платформы (MES/MDM).
    /// Принимает строковое имя действия СУБД или Enum и возвращает системный цвет палитры MudBlazor.
    /// </summary>
    public virtual Color GetActionColor(string? action)
    {
        if (string.IsNullOrWhiteSpace(action)) return Color.Default;

        return action.ToLower().Trim() switch
        {
            "create" or "insert" or "added" or "добавление" or "создание" => Color.Success,
            "update" or "edit" or "modified" or "изменение" or "редактирование" => Color.Warning,
            "delete" or "remove" or "deleted" or "softdeleted" or "удаление" => Color.Error,
            _ => Color.Default
        };
    }

    /// <summary>
    /// Универсальный платформенный высокопроизводительный движок клонирования.
    /// Создает точную изолированную глубокую копию любого полиморфного доменного объекта СУБД для безопасного редактирования в диалогах.
    /// Автоматически вырезает бесконечные ORM-петли (Parent/Children) и обходит ограничения абстрактных типов.
    /// </summary>
    /// <typeparam name="T">Базовый или конкретный тип доменной сущности</typeparam>
    public T CloneEntity<T>(T entity) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            // Базовая защита от стандартных циклических ссылок
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,

            // Тотальный фильтр: вырезаем любые сложные навигационные свойства домена из JSON-пайплайна
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
            {
                Modifiers = { typeInfo =>
            {
                foreach (var property in typeInfo.Properties)
                {
                    // 1. Если это навигационные свойства иерархий деревьев
                    if (property.Name is "Parent" or "Children")
                    {
                        property.ShouldSerialize = (_, _) => false;
                        continue;
                    }

                    // 2. Универсальный фильтр для ГРИДОВ: 
                    // Если свойство является сложным доменным объектом бэкенда или коллекцией (связи One-to-Many / Many-to-One),
                    // мы не тащим его в клон формы, оставляя только плоские прикладные поля и ID
                    Type propType = property.PropertyType;
                    bool isDomainClass = typeof(Domain.DomainObject).IsAssignableFrom(propType);
                    bool isDomainCollection = propType.IsGenericType &&
                                              typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) &&
                                              typeof(Domain.DomainObject).IsAssignableFrom(propType.GetGenericArguments().FirstOrDefault());

                    if (isDomainClass || isDomainCollection)
                    {
                        property.ShouldSerialize = (_, _) => false;
                    }
                }
            }}
            }
        };

        // Сериализуем и десериализуем, строго сохраняя реальный рантайм-тип наследника (для полиморфных таблиц/деревьев)
        string json = System.Text.Json.JsonSerializer.Serialize(entity, entity.GetType(), jsonOptions);
        return (T)System.Text.Json.JsonSerializer.Deserialize(json, entity.GetType(), jsonOptions)!;
    }
}