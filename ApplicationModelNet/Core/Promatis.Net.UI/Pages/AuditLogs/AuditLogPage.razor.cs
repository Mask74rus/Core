using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.Workspaces;

namespace Promatis.Net.UI.Pages.AuditLogs;



public partial class AuditLogPage : ComponentBase, IDisposable
{
    private bool _isDisposed;

    /// <summary>
    /// Ссылка на экземпляр таблицы MudBlazor из разметки .razor.
    /// </summary>
    protected GridPage<AuditLog>? _grid;

    [Inject]
    protected IServiceProvider PageServiceProvider { get; set; } = null!;

    /// <summary>
    /// Прямое, строго типизированное свойство контекста логов аудита (без кастов в коде).
    /// </summary>
    protected AuditLogContext Context { get; set; } = null!;

    /// <summary>
    /// Фаза инициализации страницы Blazor Server. Рождение стейт-контейнера и настройка реактивности.
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 0 мс ИНИЦИАЛИЗАЦИЯ: Выделяем память под контекст экрана
        Context = new AuditLogContext(PageServiceProvider);

        // Подписываемся на ЕДИНЫЙ открытый реактивный пульс контекста логов
        Context.OnContextUpdated += HandleContextUpdated;
    }

    /// <summary>
    /// Фаза ленивой инициализации после гарантированной первичной отрисовки разметки.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // Включаем gRPC транспорт данных и запускаем асинхронную подгрузку метаданных комбобоксов
            await Context.ActivateTransportAsync();
        }
    }

    /// <summary>
    /// Центральный диспетчер реактивности экрана логов. 
    /// Вызывается как при смене IsLoading, так и при любом изменении комбобоксов фильтров.
    /// Мягко запускает перерисовку Blazor Server, заставляя таблицу нативно обновить строки.
    /// </summary>
    private void HandleContextUpdated()
    {
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Гарантированная зачистка подписок для предотвращения утечек оперативной памяти на сервере.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        if (Context != null)
        {
            Context.OnContextUpdated -= HandleContextUpdated;
        }

        _isDisposed = true;
    }
}