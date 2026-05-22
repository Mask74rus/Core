using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.UI.Components.Dialogs;

namespace Promatis.Net.Test.MDM.UI.Unit;

public partial class UnitEditDialog : ComponentBase
{
    protected BaseEditDialog _dialog = null!;
    protected string _title = string.Empty;
    protected List<UnitType> _availableTypes = new();

    [Inject] protected IValidator UnitValidator { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public bool IsNew { get; set; }
    [Parameter] public UnitBase Model { get; set; } = null!;
    [Parameter] public Func<UnitBase, Task> OnSave { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // ИСПРАВЛЕНО: Читаем описание Kind напрямую из доменного атрибута через метод расширения
        _title = IsNew ? $"Создание: {Model.Kind.GetDescription()}" : $"Редактирование: {Model.Name}";

        _availableTypes = Enum.GetValues<UnitType>()
            .Where(t => t != UnitType.None)
            .Where(t => ((int)Model.Kind & (int)t) != 0)
            .ToList();
    }

    protected async Task OnSaveInternal()
    {
        if (OnSave != null)
        {
            await OnSave(Model);
        }
    }
}