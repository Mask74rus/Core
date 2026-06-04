using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public abstract partial class ReferencePage<TEntity> : ComponentBase, IDisposable
    where TEntity : Promatis.Net.Domain.ReferenceBase, new()
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

        // Подписываемся на события изменения стейта контекста для реактивной перерисовки тулбара (кнопок)
        Context.OnContextStateChanged += RefreshUi;

        // Связываем событие фиксации данных (коммита СУБД от триггеров) с мягкой перезагрузкой грида
        Context.OnContextUpdated = RefreshGrid;
    }

    // ИСПРАВЛЕНО (Железное Архитектурное Правило): Блокирующий метод OnInitializedAsync()
    // и ручной вызов старого метода LoadInitialDataAsync() ПОЛНОСТЬЮ УДАЛЕНЫ!
    // Интерфейс открывается мгновенно, а данные лениво запрашиваются сеткой после отрисовки.

    /// <summary>
    /// Метод принудительного обновления данных в MudDataGrid. Вызывается брокером при сигналах СУБД.
    /// </summary>
    protected void RefreshGrid()
    {
        // Используем встроенный потокобезопасный механизм Blazor для вызова из фоновых потоков СУБД
        InvokeAsync(async () =>
        {
            if (_grid != null)
            {
                await _grid.ReloadServerData();
            }
        });
    }

    private void RefreshUi()
    {
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Полная очистка ресурсов для предотвращения утечек памяти в Blazor
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        if (Context != null)
        {
            Context.OnContextStateChanged -= RefreshUi;

            // Если контекст создается внутри жизненного цикла формы (не через глобальный Scope DI),
            // вызываем утилизацию для отписки Брокера от шины СУБД.
            Context.Dispose();
        }

        _isDisposed = true;
    }
}