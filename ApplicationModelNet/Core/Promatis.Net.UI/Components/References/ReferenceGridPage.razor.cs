using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain.Interface;
using Promatis.Net.Service;
using Promatis.Net.UI.Components.Workspaces;

namespace Promatis.Net.UI.Components.References;

public partial class ReferenceGridPage<TEntity> : ComponentBase
    where TEntity : class, IDomainObjectHasKey<Guid>, new()
{


    protected GridPage<TEntity>? _grid;

    [Inject] protected IReferenceService<TEntity> ReferenceService { get; set; } = null!;

    [Inject] protected IDialogService DialogService { get; set; } = null!;

    protected ReferenceWorkspaceContext<TEntity> Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Context = new ReferenceWorkspaceContext<TEntity>(ReferenceService, DialogService, onStateChangedNotifier: RefreshGrid);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await Context.LoadInitialDataAsync();
            RefreshGrid();
        }
    }

    protected void RefreshGrid()
    {
        if (_grid == null) return;

        // ИСПРАВЛЕНО: Строго через InvokeAsync, чтобы перерисовка таблицы встала в очередь 
        // ПОСЛЕ того, как Blazor Server закончит рендерить кадр открывающегося диалога
        InvokeAsync(async () =>
        {
            if (_grid != null)
            {
                await _grid.ReloadServerData();
            }
        });
    }
}