using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class WorkspacePage : ComponentBase, IDisposable
{
    private IWorkspaceContext? _previousContext;
    private bool _isDisposed;

    /// <summary>
    /// Единый каскадный параметр контекста. 
    /// Теперь холст гибко отслеживает его подмену при переходах между страницами справочников.
    /// </summary>
    [CascadingParameter]
    protected IWorkspaceContext? ActionContext { get; set; }

    [Parameter] public RenderFragment? BodyContent { get; set; }
    [Parameter] public RenderFragment? TopContent { get; set; }
    [Parameter] public RenderFragment? BottomContent { get; set; }
    [Parameter] public RenderFragment? LeftContent { get; set; }
    [Parameter] public RenderFragment? RightContent { get; set; }

    /// <summary>
    /// ИСПРАВЛЕНО (Проблема №5): Перенос логики в OnParametersSet.
    /// Blazor вызывает этот метод каждый раз, когда CascadingParameter обновляется или подменяется.
    /// </summary>
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Если инстанс контекста изменился (произошел переход на другую страницу справочника)
        if (ActionContext != _previousContext)
        {
            // 1. Безопасно отписываемся от событий старого контекста
            if (_previousContext != null)
            {
                _previousContext.OnContextStateChanged -= HandleContextStateChanged;
            }

            // 2. Запоминаем новый контекст как текущий
            _previousContext = ActionContext;

            if (ActionContext != null)
            {
                // 3. Подписываемся на реактивные изменения нового контекста
                ActionContext.OnContextStateChanged += HandleContextStateChanged;

                // 4. ИСПРАВЛЕНО (Проблема №4): Безопасный триггер запуска жизненного цикла!
                // Инициализируем контекст (сборка тулбаров, настройка Брокеров) только сейчас,
                // когда и холст, и все прикладные наследники на 100% созданы в памяти.
                ActionContext.InitializeContext();
            }
        }
    }

    /// <summary>
    /// Обработчик реактивного изменения состояния. 
    /// При любом шорохе в ОЗУ-кэше или переключении IsLoading заставляет Blazor перерисовать 5 зон.
    /// </summary>
    private void HandleContextStateChanged()
    {
        if (!_isDisposed)
        {
            InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// ИСПРАВЛЕНО (Проблема №6): Безопасный Dispose.
    /// Гарантирует отсутствие падений NullReferenceException при закрытии статических страниц.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        if (ActionContext != null)
        {
            ActionContext.OnContextStateChanged -= HandleContextStateChanged;
        }

        _isDisposed = true;
    }

    // --- Безопасная проброска параметров геометрии и разметки ---
    protected int GetPaperElevation() => ActionContext?.PaperElevation ?? 1;
    protected string GetPaperClass() => ActionContext?.PaperClass ?? "pa-4 d-flex flex-column flex-grow-1 w-100";
    protected string GetWorkspaceHeight() => ActionContext?.WorkspaceHeight ?? "100%";
    protected string GetTopHeight() => ActionContext?.TopZoneHeight ?? "auto";
    protected string GetBottomHeight() => ActionContext?.BottomZoneHeight ?? "auto";
    protected string GetLeftWidth() => ActionContext?.LeftZoneWidth ?? "250px";
    protected string GetRightWidth() => ActionContext?.RightZoneWidth ?? "300px";

    // --- Логика автоматического схлопывания пустых зон (Collapsing) ---
    protected bool IsTopCollapsed() => TopContent == null || (ActionContext?.IsTopZoneCollapsed ?? false);
    protected bool IsBottomCollapsed() => BottomContent == null || (ActionContext?.IsBottomZoneCollapsed ?? false);
    protected bool IsLeftCollapsed() => LeftContent == null || (ActionContext?.IsLeftZoneCollapsed ?? false);
    protected bool IsRightCollapsed() => RightContent == null || (ActionContext?.IsRightZoneCollapsed ?? false);
}