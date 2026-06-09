using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Promatis.Net.UI.Components.Workspaces;

namespace Promatis.Net.UI.Pages.AuditLogs;



public partial class AuditLogPage : ComponentBase, IDisposable
{
    private GridPage<AuditLog>? _grid;
    private bool _isDisposed;

    [Inject]
    protected IServiceProvider PageServiceProvider { get; set; } = null!;

    protected AuditLogContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // 0 мс ИНИЦИАЛИЗАЦИЯ
        Context = new AuditLogContext(PageServiceProvider);

        // Подписка 1: Только на общие изменения (оверлеи загрузки)
        Context.OnContextUpdated += HandleContextUpdated;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            // Подписка 2: На адресный сигнал изменения фильтров ИЗ КОНТЕКСТА
            Context.OnFiltersChanged += OnFiltersTriggered;

            // Включаем Брокер и качаем метаданные СУБД внутри контекста
            await Context.ActivateTransportAsync();

            StateHasChanged();
        }
    }

    /// <summary>
    /// АДРЕСНЫЙ ПЕРЕХВАТ: Срабатывает только тогда, когда контекст сообщил о мутации фильтров.
    /// Перезагружает пассивный грид без бесконечных петель!
    /// </summary>
    private void OnFiltersTriggered()
    {
        InvokeAsync(async () =>
        {
            if (_grid != null)
            {
                await _grid.ReloadServerData();
            }
        });
    }

    private void HandleContextUpdated()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        if (Context != null)
        {
            Context.OnContextUpdated -= HandleContextUpdated;
            Context.OnFiltersChanged -= OnFiltersTriggered;
        }

        _isDisposed = true;
    }
}