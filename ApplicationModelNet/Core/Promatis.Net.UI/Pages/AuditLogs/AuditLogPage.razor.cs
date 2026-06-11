using Microsoft.AspNetCore.Components;
using Promatis.Net.Domain;
using Promatis.Net.UI.Components.Workspaces;

namespace Promatis.Net.UI.Pages.AuditLogs;



public partial class AuditLogPage : ComponentBase, IDisposable
{
    private bool _isDisposed;
    protected GridPage<AuditLog>? _grid;

    [Inject] protected IServiceProvider PageServiceProvider { get; set; } = null!;
    protected AuditLogContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Context = new AuditLogContext(PageServiceProvider);

        // ПОДПИСКА 1: Общий пульс UI. Отвечает только за оверлеи загрузки и IsLoading холста
        Context.OnContextUpdated += HandleContextUpdated;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // ПОДПИСКА 2: Адресный сигнал фильтров. Страница ловит его в легитимном UI-потоке Blazor
            // и безопасно через родной InvokeAsync командует таблице перезагрузиться!
            Context.OnFiltersChanged += HandleFiltersTriggered;

            await Context.ActivateTransportAsync();
        }
    }

    /// <summary>
    /// АДРЕСНЫЙ ПЕРЕХВАТ МУТАЦИИ ФИЛЬТРОВ.
    /// Выполняется строго в контексте синхронизации Blazor Server через InvokeAsync.
    /// Запускает нативный сброс кэша MudBlazor и выполняет свежий gRPC-запрос.
    /// </summary>
    private void HandleFiltersTriggered()
    {
        // Запускаем асинхронный маршаллинг. 
        // Сам метод HandleFiltersTriggered остается void (Fire-and-Forget), 
        // полностью развязывая поток страницы и gRPC-транспорт Брокера.
        _ = InvokeAsync(async () =>
        {
            if (_grid != null)
            {
                await _grid.ReloadServerData();
            }
        });
    }

    /// <summary>
    /// Обработчик пассивной перерисовки элементов холста (выключение крутилки загрузки).
    /// </summary>
    private void HandleContextUpdated()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        Context.OnContextUpdated -= HandleContextUpdated;
        Context.OnFiltersChanged -= HandleFiltersTriggered;
        _isDisposed = true;
    }
}