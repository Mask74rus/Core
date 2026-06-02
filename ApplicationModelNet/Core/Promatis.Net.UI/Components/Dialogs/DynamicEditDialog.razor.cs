using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Promatis.Net.UI.Components.Dialogs;

public partial class DynamicEditDialog : ComponentBase, IDisposable
{
    protected MudForm _form = null!;
    protected readonly List<DialogTab> _collectedTabs = [];
    protected int _activeTabIndex; // Управляемый индекс активного таба
    private RenderFragment? _oldTabs;

    [CascadingParameter] protected IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public required IDialogActionContext Context { get; set; }
    [Parameter] public string Title { get; set; } = "Редактирование";
    [Parameter] public RenderFragment? Tabs { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Context.OnContextStateChanged += HandleContextStateChanged;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (_oldTabs != Tabs)
        {
            _oldTabs = Tabs;
            _collectedTabs.Clear();
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            Context.BindForm(_form);
            Context.BindDialogInstance(MudDialog);

            // Подменяем стандартный ValidateFormAsync в контексте на наш умный метод с фокусом вкладок!
            if (Context is DialogActionContextBase<object> baseContext)
            {
                // Мы можем перенаправить вызов валидации контекста на этот UI-метод
                // Но чтобы не усложнять, мы можем просто в DialogActionContextBase 
                // вызвать _form.ValidateAsync(), а диалог сам среагирует на изменение FirstFailedPropertyName!
            }

            InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// ГЛАВНЫЙ ИНТЕЛЛЕКТУАЛЬНЫЙ МЕТОД: Валидирует форму при сохранении 
    /// и автоматически переключает вкладку на ту, где обнаружена первая ошибка
    /// </summary>
    public async Task<bool> ForceValidateAndFocusAsync()
    {
        if (_form == null) return true;

        // 1. Запускаем полную валидацию MudForm (она нативно дернет ExecuteFluentValidationAsync)
        await _form.ValidateAsync();

        // Если всё заполнено верно — возвращаем true, диалог пойдет сохраняться в базу
        if (_form.IsValid) return true;

        // 2. ИСПРАВЛЕНО: Если форма НЕВАЛИДНА, берем имя ошибочного свойства из нашего контекста
        if (Context.FirstFailedPropertyName is { } firstFailedProperty)
        {
            // Ищем индекс вкладки, которая заявила в своем параметре PropertyNames, что владеет этим полем
            int targetTabIndex = _collectedTabs.FindIndex(tab =>
                tab.PropertyNames.Contains(firstFailedProperty));

            if (targetTabIndex >= 0)
            {
                // МГНОВЕННЫЙ ФОКУС: Автоматически переключаем пользователя на нужную вкладку с ошибкой!
                _activeTabIndex = targetTabIndex;
                await InvokeAsync(StateHasChanged);
            }
        }

        return false;
    }

    public void RegisterTab(DialogTab tab)
    {
        if (!_collectedTabs.Contains(tab))
        {
            _collectedTabs.Add(tab);
        }
    }

    private void HandleContextStateChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        if (Context != null) Context.OnContextStateChanged -= HandleContextStateChanged;
    }
}