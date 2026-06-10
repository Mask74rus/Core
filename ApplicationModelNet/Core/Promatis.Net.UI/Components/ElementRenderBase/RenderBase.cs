using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.ElementRenderBase;

/// <summary>
/// Абстрактный базовый компонент визуализации элементов управления (кнопок, полей ввода).
/// Обеспечивает строгое ООП-взаимодействие с контекстом данных без использования рефлексии.
/// </summary>
public abstract class RenderBase : ComponentBase, IDisposable
{
    /// <summary>
    /// Абстрактная модель элемента управления, содержащая бизнес-логику и флаги доступности.
    /// </summary>
    [Parameter]
    public IUiControl Control { get; set; } = null!;

    /// <summary>
    /// Захват контекста данных из каскада. Благодаря наследованию интерфейсов-матрешек,
    /// сюда прозрачно прилетит любой контекст страницы (ReferenceContext, TreeContext, AuditLogContext).
    /// </summary>
    [CascadingParameter]
    protected IEntityContext? EntityContext { get; set; }

    /// <summary>
    /// Чистый ООП-контракт извлечения выделенной строки в виде object? напрямую из интерфейса.
    /// </summary>
    protected object? CurrentSelectedData => EntityContext?.SelectedData;

    /// <summary>
    /// Фаза инициализации компонента Blazor Server. Настраивает сквозную событийную реактивность.
    /// </summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Подписка 1: Если модель самой кнопки изменила состояние (например, IsRunning или IsEnabled)
        Control.OnStateChanged += HandleStateChanged;

        // Подписка 2: Если пользователь выбрал другую строку в таблице или узел в дереве,
        // кнопка обязана мгновенно узнать об этом для пересчета метода IsEnabledForData
        if (EntityContext != null)
        {
            EntityContext.OnContextUpdated += HandleStateChanged;
        }
    }

    /// <summary>
    /// Потокобезопасный вызов перерисовки компонента в контексте синхронизации Blazor Server.
    /// </summary>
    private void HandleStateChanged() => InvokeAsync(StateHasChanged);

    /// <summary>
    /// Освобождение системных ресурсов и гарантированная отписка от событий для предотвращения утечек памяти.
    /// </summary>
    public virtual void Dispose()
    {
        Control.OnStateChanged -= HandleStateChanged;

        if (EntityContext != null)
        {
            EntityContext.OnContextUpdated -= HandleStateChanged;
        }
    }
}