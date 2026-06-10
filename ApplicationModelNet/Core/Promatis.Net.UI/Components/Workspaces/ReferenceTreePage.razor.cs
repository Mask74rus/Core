using Microsoft.AspNetCore.Components;
using MudBlazor;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Promatis.Net.UI.Components.Workspaces;

/// <summary>
/// Базовая C#-логика страницы древовидных справочников платформы.
/// Полностью очищена от избыточных проверок, рефлексии и защищена от бесконечных циклов рендеринга.
/// </summary>
/// <typeparam name="TEntity">Бизнес-сущность дерева (наследник ReferenceTreeBase).</typeparam>
public abstract partial class ReferenceTreePage<TEntity> : ComponentBase, IDisposable
    where TEntity : Domain.ReferenceTreeBase<TEntity>, new()
{
    private bool _isDisposed;

    [Inject]
    protected IServiceProvider PageServiceProvider { get; set; } = null!;

    /// <summary>
    /// Строго типизированный иерархический контекст управления этим деревом.
    /// </summary>
    protected abstract Components.ReferenceTreeContext<TEntity> Context { get; }

    /// <summary>
    /// Фаза инициализации страницы Blazor Server. Связывает единый реактивный пульс UI и ЯДРА.
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // СВЯЗУЮЩИЙ МОСТ РЕАКТИВНОСТИ: Подписываемся на ЕДИНЫЙ открытый пульс ядра!
        Context.OnContextUpdated += HandleContextUpdated;
    }

    /// <summary>
    /// Фаза ленивой инициализации. Запускает первичный транспорт данных.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Активируем gRPC транспорт данных. 
            // Контекст сам скачает плоский список, сам соберет TreeGraph и поднимет событие обновления!
            await Context.ActivateTransportAsync();
        }
    }

    /// <summary>
    /// Центральный диспетчер реактивности экрана. 
    /// Вызывается при любых мутациях (клик по узлу, триггер СУБД, завершение загрузки).
    /// Просто дает Blazor Server команду перерисовать интерфейс на основе актуального стейта контекста.
    /// </summary>
    private void HandleContextUpdated()
    {
        // ЧИСТЫЙ БЛЕЙЗОР: Никаких ифов, никаких повторных вызовов методов загрузки и мертвых петель!
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        Context.OnContextUpdated -= HandleContextUpdated;

        _isDisposed = true;
    }
}