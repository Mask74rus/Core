using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;
using Promatis.Net.UI.Components.Workspaces;

namespace Promatis.Net.UI.Pages.AuditLogs;

public partial class AuditLogPage : ComponentBase
{
    private GridPage<AuditLog>? _grid;
    private bool _isLoaded;

    [Inject]
    protected IAuditLogService AuditLogService { get; set; } = null!;

    protected AuditLogWorkspaceContext Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // МГНОВЕННО создаем пустой контекст. Страница рендерится без задержек за 0 мс
        Context = new AuditLogWorkspaceContext(AuditLogService, onFilterChanged: RefreshGrid);
    }

    /// <summary>
    /// Каноническая точка запуска тяжелых запросов. Срабатывает ПОСЛЕ того, 
    /// как пользователь уже увидел готовую страницу на экране.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        // Выполняем загрузку только один раз при первом рендере экрана
        if (firstRender)
        {
            // Спокойно скачиваем уникальные имена сущностей из СУБД PostgreSQL
            List<string> availableEntities = await AuditLogService.GetAvailableEntityNamesAsync();

            // Передаем данные в контекст для динамической генерации селекта и пикера периодов
            Context.InitializeFilters(availableEntities);

            _isLoaded = true;
            StateHasChanged(); // Перерисовываем тулбар и запускаем первую загрузку логов
        }
    }

    protected void RefreshGrid()
    {
        // Запрещаем принудительное обновление грида от триггеров фильтров,
        // пока эти фильтры еще первично не настроены в OnAfterRenderAsync
        if (_isLoaded && _grid != null)
        {
            _ = _grid.ReloadServerData();
        }
    }
}