using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Service;


namespace Promatis.Net.MES.MDM.UI.Page.TechnologicalParameter;

public partial class TechnologicalParameterDialog : ComponentBase
{
    [Inject] protected IValidator<Domain.TechnologicalParameter> Validator { get; set; } = null!;
    [Inject] protected IReferenceService<Domain.TechnologicalParameter> ParameterService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public Domain.TechnologicalParameter Model { get; set; } = null!;
    [Parameter] public bool IsNew { get; set; }

    protected ReferenceBaseValidator<Domain.TechnologicalParameter> _parameterValidator = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Приведение типов для получения доступа к делегату потоковой валидации инпутов
        if (Validator is ReferenceBaseValidator<Domain.TechnologicalParameter> referenceValidator)
        {
            _parameterValidator = referenceValidator;
        }
        else
        {
            throw new InvalidOperationException(
                $"Валидатор для {nameof(TechnologicalParameter)} должен наследоваться от {nameof(ReferenceBaseValidator<>)}");
        }
    }

    /// <summary>
    /// Делегат сохранения, который автоматически вызывается внутри BaseEditDialog
    /// </summary>
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