using FluentValidation;
using Microsoft.AspNetCore.Components;
using Promatis.Net.Domain;

namespace Promatis.Net.UI.Components.Dialogs;

public partial class ReferenceDialogLayout<TModel> : ComponentBase where TModel : ReferenceBase
{
    protected int _activeTabIndex;

    /// <summary>
    /// Текстовый заголовок в шапке модального окна. Пробрасывается напрямую в BaseDialogLayout.
    /// </summary>
    [Parameter]
    public string Title { get; set; } = "Редактирование справочника";

    /// <summary>
    /// Живая редактируемая доменная модель (наследник ReferenceBase).
    /// </summary>
    [Parameter]
    public required TModel Model { get; set; }

    /// <summary>
    /// Полиморфный Fluent-валидатор для конкретного справочника.
    /// </summary>
    [Parameter]
    public IValidator<TModel>? Validator { get; set; }

    /// <summary>
    /// Коллекция строго типизированных кастомных вкладок (дескрипторов), 
    /// которые конкретный справочник хочет добавить на форму в дополнение к вкладке "Основное".
    /// </summary>
    [Parameter]
    public IReadOnlyCollection<DialogTabConfig<TModel>>? CustomTabs { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Model == null)
            throw new ArgumentNullException(nameof(Model), "Параметр Model является обязательным для ReferenceDialogLayout.");
    }
}