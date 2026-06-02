using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Dialogs;

public partial class DynamicEditDialog : ComponentBase
{
    protected MudForm _form = null!;
    protected readonly List<DialogTab> _collectedTabs = [];

    [CascadingParameter]
    protected IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public required IDialogActionContext Context { get; set; }

    [Parameter]
    public string Title { get; set; } = "Редактирование";

    /// <summary>
    /// Сюда прикладной разработчик декларативно пишет теги <DialogTab>
    /// </summary>
    [Parameter]
    public RenderFragment? Tabs { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Context == null)
            throw new ArgumentNullException(nameof(Context), "Интерфейс IDialogActionContext должен быть передан в компонент диалога.");

        // Подписываемся на реактивное изменение состояния контекста для перерисовки окон
        Context.OnContextStateChanged += HandleContextStateChanged;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        // Очищаем коллекцию вкладок перед каждым рендером параметров, чтобы избежать их дублирования
        _collectedTabs.Clear();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            // ЖЕНИМ МЕХАНИКУ: Передаем контексту ссылки на MudForm и инстанс диалога MudBlazor
            Context.BindForm(_form);
            Context.BindDialogInstance(MudDialog);
        }
    }

    public void RegisterTab(DialogTab tab)
    {
        if (!_collectedTabs.Contains(tab))
        {
            _collectedTabs.Add(tab);
            StateHasChanged();
        }
    }

    private void HandleContextStateChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (Context != null)
        {
            Context.OnContextStateChanged -= HandleContextStateChanged;
        }
    }
}