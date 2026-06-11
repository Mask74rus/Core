using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;


/// <summary>
/// Базовая C#-логика страницы табличных справочников платформы.
/// Полностью очищена от избыточных проверок и рефлексии. Работает на чистых ООП-контрактах.
/// </summary>
/// <typeparam name="TEntity">Бизнес-сущность справочника (наследник ReferenceBase).</typeparam>
public abstract partial class ReferencePage<TEntity> : ComponentBase, IDisposable
    where TEntity : Domain.ReferenceBase, new()
{
    private bool _isDisposed;

    /// <summary>
    /// Ссылка на экземпляр табличной сетки MudBlazor из разметки .razor.
    /// </summary>
    protected GridPage<TEntity>? _grid;

    [Inject]
    protected IServiceProvider PageServiceProvider { get; set; } = null!;

    /// <summary>
    /// Строго типизированный контекст управления этим справочником.
    /// </summary>
    protected abstract Components.ReferenceContext<TEntity> Context { get; }

    /// <summary>
    /// Слот для декларативного описания уникальных колонок конкретной таблицы.
    /// </summary>
    [Parameter]
    public RenderFragment? CustomColumns { get; set; }

    /// <summary>
    /// Фаза инициализации страницы Blazor Server. Связывает реактивные пульсы UI и ЯДРА.
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // СВЯЗУЮЩИЙ МОСТ РЕАКТИВНОСТИ: Подписываемся на открытый пульс стейта.
        // Сюда прилетают и флаги IsLoading, и сигналы коммитов СУБД от Брокера.
        Context.OnContextUpdated += HandleContextUpdated;
    }

    /// <summary>
    /// Фаза ленивой инициализации после гарантированной отрисовки разметки на экране.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Активируем gRPC транспорт. Брокер встает в строй на основе конфигурации, заданной в конструкторе.
            await Context.ActivateTransportAsync();
        }
    }

    /// <summary>
    /// Центральный диспетчер реактивности визуального экрана. 
    /// Отвечает за полную синхронизацию пассивного UI MudBlazor (кнопки, оверлеи, строки таблицы).
    /// </summary>
    private void HandleContextUpdated()
    {
        // Мягко пинаем поток рендеринга Blazor Server.
        // Таблица нативно перечитает изменившийся Ozu-кэш без спама gRPC-запросами.
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Полная очистка ресурсов для предотвращения утечек памяти.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        Context.OnContextUpdated -= HandleContextUpdated;

        _isDisposed = true;
    }
}