using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;


namespace Promatis.Net.MES.MDM.UI.Page.TechnologicalParameter;

public partial class TechnologicalParameterDialog : ComponentBase
{
    // Инжектируем не-дженерик IValidator, чтобы избежать конфликтов приведения типов
    [Inject] protected IValidator Validator { get; set; } = null!;
    [Inject] protected IReferenceService<Domain.TechnologicalParameter> ParameterService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public Domain.TechnologicalParameter Model { get; set; } = null!;
    [Parameter] public bool IsNew { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // ИСПРАВЛЕНО: Жёсткое приведение типов к ReferenceBaseValidator удалено. 
        // Вся инлайн и кросс-валидация полностью делегирована платформенному механизму BaseEditDialog.
    }

    protected async Task SaveParameterAsync()
    {
        if (IsNew)
        {
            await ParameterService.AddAsync(Model);
        }
        else
        {
            await ParameterService.UpdateAsync(Model);
        }
    }
}