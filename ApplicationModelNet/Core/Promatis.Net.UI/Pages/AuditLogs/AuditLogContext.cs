using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Data;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Pages.AuditLogs.Toolbar;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Promatis.Net.UI.Pages.AuditLogs;

/// <summary>
/// Контекст экрана системного аудита.
/// Жестко сконфигурирован на Server-Side режим работы для прямого постраничного gRPC-поиска по PostgreSQL.
/// </summary>
public class AuditLogContext : Components.GridContext<AuditLog>, Components.IToolbarContext
{
    private readonly IAuditLogService _auditLogService;

    // СТРОГО ТИПИЗИРОВАННЫЕ КОМПОНЕНТЫ ФИЛЬТРАЦИИ НА ТУЛБАРЕ
    public AuditActionSelect ActionFilter { get; }
    public AuditPeriodPicker PeriodFilter { get; }
    public AuditEntitySelect EntityFilter { get; }

    // ====================================================================================
    // --- РЕАЛИЗАЦИЯ ФИЗИЧЕСКОЙ ПАМЯТИ ДЛЯ КОНТРАКТА IToolbarContext ---
    // ====================================================================================
    public System.Threading.Lock ControlsLock { get; } = new();
    public List<IUiControl> InnerControls { get; } = [];
    public bool IsToolbarInitialized { get; set; }

    /// <summary>
    /// Переопределяем высоту верхней зоны для комфортного размещения трех комбобоксов фильтров.
    /// </summary>
    public override string TopZoneHeight => "48px";

    /// <summary>
    /// Конструктор контекста логов аудита. 
    /// ХИРУРГИЧЕСКИ ИСПРАВЛЕНО: Убран флаг isInMemoryMode, внедрена декларативная конфигурация сервера.
    /// </summary>
    public AuditLogContext(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _auditLogService = serviceProvider.GetRequiredService<IAuditLogService>();

        // 1. Инициализируем компоненты фильтров, передавая им делегат реактивного отклика
        ActionFilter = new AuditActionSelect(HandleFilterMutation);
        PeriodFilter = new AuditPeriodPicker(HandleFilterMutation);
        EntityFilter = new AuditEntitySelect(new List<string>(), HandleFilterMutation);

        // 2. ЖЕЛЕЗНАЯ КОНФИГУРАЦИЯ ЯДРА (БЕЗ ФЛАГОВ):
        // Жестко заявляем Брокеру, что этот экран работает исключительно в Server-Side режиме,
        // и передаем ссылку на чистый метод постраничного gRPC-транспорта.
        ConfigureUsingServerSideMode(FetchDataFromServerInternalAsync);
    }

    /// <summary>
    /// Выделенное событие мутации критериев поиска. 
    /// Срабатывает строго тогда, когда пользователь руками изменил комбобокс на тулбаре.
    /// </summary>
    public event Action? OnFiltersChanged;

    /// <summary>
    /// ВНУТРЕННИЙ ДИСПЕТЧЕР МУТАЦИИ ФИЛЬТРОВ.
    /// Вызывается комбобоксами тулбара. Чистое синхронное ООП-решение без async void и без красного InvokeAsync!
    /// </summary>
    private void HandleFilterMutation()
    {
        // 1. Пинаем тулбар на перерисовку (чтобы комбобокс зафиксировал выбранный текст)
        NotifyContextUpdated();

        // 2. Выбрасываем адресный импульс для страницы. Страница поймает его в своем потоке Blazor!
        OnFiltersChanged?.Invoke();
    }

    /// <summary>
    /// Ленивое наполнение тулбара комбобоксами и кнопками экспорта.
    /// </summary>
    public void PopulateDefaultToolbar(List<IUiControl> controls)
    {
        controls.Add(EntityFilter);
        controls.Add(ActionFilter);
        controls.Add(PeriodFilter);
        controls.Add(new AuditToolbarDivider());
        controls.Add(new AuditExportButton());
    }

    /// <summary>
    /// ФАЗА А (ХУК ЯДРА): Асинхронное ленивое наполнение опций выпадающего меню комбобокса.
    /// Выполняется параллельно, не блокируя стартовый вывод таблицы.
    /// </summary>
    protected override async Task LoadMetadataInternalAsync()
    {
        List<string> availableEntities = await _auditLogService.GetAvailableEntityNamesAsync();

        var list = new List<string> { "Все сущности" };
        list.AddRange(availableEntities);

        // Просто отдали опции в UI-элемент. Всплесков и перезагрузок отсюда слать нельзя.
        EntityFilter.Options = list;
    }

    /// <summary>
    /// ФАЗА Б: Чистый серверный транспорт данных таблицы.
    /// ХИРУРГИЧЕСКИ ИСПРАВЛЕНО: Убран ошибочный override. Метод инкапсулирован внутри лямбда-конфигуратора.
    /// Читает строго ТЕКУЩИЕ выбранные значения (Value) из объектов фильтров тулбара.
    /// </summary>
    private async Task<GridData<AuditLog>> FetchDataFromServerInternalAsync(GridState<AuditLog> state, CancellationToken ct)
    {
        // Читаем выбранные пользователем критерии (Value)
        string? selectedAction = ActionFilter.GetSelectedActionValue();
        DateRange? selectedPeriod = PeriodFilter.Value as DateRange;
        string? selectedEntity = EntityFilter.Value as string;

        if (selectedEntity == "Все сущности") selectedEntity = null;

        // Физика дат: если период не выбран, берем глубину в 7 дней по умолчанию
        DateTime fromDate = selectedPeriod?.Start ?? DateTime.Today.AddDays(-7);
        DateTime toDate = selectedPeriod?.End ?? DateTime.Today;

        // Принудительно выравниваем Kind для gRPC-сериализации в UTC контракты PostgreSQL
        fromDate = DateTime.SpecifyKind(fromDate.Date, DateTimeKind.Utc);
        toDate = DateTime.SpecifyKind(toDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var searchRequest = new AuditLogSearchRequest(
            FromDate: fromDate,
            ToDate: toDate,
            EntityName: selectedEntity,
            Action: selectedAction,
            PageIndex: state.Page,
            PageSize: state.PageSize
        );

        // Выполняем прямой gRPC запрос к микросервису
        PagedResult<AuditLog> pagedResult = await _auditLogService.SearchLogsAsync(searchRequest, ct);

        return new GridData<AuditLog>
        {
            Items = pagedResult.Items ?? [],
            TotalItems = pagedResult.TotalCount
        };
    }

}