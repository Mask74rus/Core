using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public abstract partial class ReferencePage<TEntity> : ComponentBase, IDisposable
    where TEntity : Domain.ReferenceBase, new()
{
    protected GridPage<TEntity>? _grid;
    private bool _isDisposed;

    [Inject]
    protected IServiceProvider PageServiceProvider { get; set; } = null!;

    /// <summary>
    /// Строго типизированный контекст управления этим справочником.
    /// </summary>
    protected abstract ReferenceContext<TEntity> Context { get; }

    /// <summary>
    /// Слот для декларативного описания уникальных колонок таблицы.
    /// </summary>
    [Parameter]
    public RenderFragment? CustomColumns { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // СВЯЗУЮЩИЙ МОСТ РЕАКТИВНОСТИ: Подписываемся на ЕДИНЫЙ открытый пульс ядра в одной точке!
        if (Context != null)
        {
            Context.OnContextUpdated += HandleContextUpdated;
        }
    }

    /// <summary>
    /// Центральный диспетчер реактивности экрана. Вызывается при ЛЮБЫХ мутациях стейта контекста.
    /// </summary>
    private void HandleContextUpdated()
    {
        InvokeAsync(async () =>
        {
            // Шаг 1: Форсируем перерисовку элементов страницы (тулбар, оверлеи загрузки)
            StateHasChanged();

            // Шаг 2: Если контекст завершил CRUD-операцию и очистил черновик,
            // даем команду пассивному гриду мягко перезагрузить данные с gRPC-сервера
            if (Context.DraftData == null && _grid != null)
            {
                await _grid.ReloadServerData();
            }
        });
    }

    /// <summary>
    /// Полная очистка ресурсов для предотвращения утечек памяти в Blazor Server
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        if (Context != null)
        {
            Context.OnContextUpdated -= HandleContextUpdated;

            // Если контекст имеет собственную логику очистки, утилизируем его
            if (Context is IDisposable disposableContext)
            {
                disposableContext.Dispose();
            }
        }

        _isDisposed = true;
    }
}