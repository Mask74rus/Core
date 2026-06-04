using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public abstract partial class ReferenceTreePage<TEntity> : ComponentBase, IDisposable
    where TEntity : Promatis.Net.Domain.ReferenceTreeBase<TEntity>, new()
{
    private bool _isDisposed;

    [Inject]
    protected IServiceProvider PageServiceProvider { get; set; } = null!;

    /// <summary>
    /// Строго типизированный иерархический контекст управления этим деревом.
    /// </summary>
    protected abstract ReferenceTreeContext<TEntity> Context { get; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Подписываемся на события изменения стейта контекста для реактивной перерисовки тулбара (кнопок)
        Context.OnContextStateChanged += RefreshUi;

        // Связываем событие фиксации данных (коммита СУБД) с мягкой перерисовкой всего экрана
        Context.OnContextUpdated = RefreshUi;
    }

    // ИСПРАВЛЕНО (Железное Архитектурное Правило): Блокирующий метод OnInitializedAsync()
    // и ручной вызов старого метода LoadInitialDataAsync() ПОЛНОСТЬЮ УДАЛЕНЫ!
    // Дерево открывается мгновенно, а иерархия лениво запрашивается после отрисовки каркаса.

    private void RefreshUi()
    {
        // Гарантируем потокобезопасный вызов перерисовки в контексте Blazor UI-потока
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

            // Вызываем утилизацию контекста для гарантированной отписки брокера от статических триггеров СУБД
            Context.Dispose();
        }

        _isDisposed = true;
    }
}