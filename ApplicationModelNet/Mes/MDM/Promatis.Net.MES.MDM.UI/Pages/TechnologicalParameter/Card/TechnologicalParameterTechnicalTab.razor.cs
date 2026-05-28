using Microsoft.AspNetCore.Components;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.MES.Service;

namespace Promatis.Net.MES.MDM.UI.Pages.TechnologicalParameter.Card;

public partial class TechnologicalParameterTechnicalTab : ComponentBase
{
    [Inject] protected IUnitOfMeasurementService UomService { get; set; } = null!;
    [Parameter] public required Domain.TechnologicalParameter Model { get; set; }

    protected readonly string[] AllowedDataTypes = ["Numeric", "String", "Boolean", "DateTime"];
    protected List<UnitOfMeasurement> _availableUoms = [];

    // ИСПРАВЛЕНО: Переведено на контракт IReadOnlyCollection согласно требованиям MudBlazor API
    protected IReadOnlyCollection<CalculationMethod> _selectedMethods = Array.Empty<CalculationMethod>();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _availableUoms = await UomService.GetAllAsync();

        // Распаковываем число побитовой маски из БД в доступный массив флагов
        _selectedMethods = Enum.GetValues(typeof(CalculationMethod))
            .Cast<CalculationMethod>()
            .Where(method => method != CalculationMethod.None && Model.AllowedMethods.HasFlag(method))
            .ToList()
            .AsReadOnly(); // Приведение к IReadOnlyCollection
    }

    /// <summary>
    /// Срабатывает при изменении галочек. 
    /// ИСПРАВЛЕНО: Аргумент приведен к типу IReadOnlyCollection для устранения ошибки компиляции.
    /// </summary>
    protected void OnAllowedMethodsChanged(IReadOnlyCollection<CalculationMethod> updatedValues)
    {
        _selectedMethods = updatedValues;

        // Схлопываем массив чекбоксов обратно в побитовое число int для СУБД
        CalculationMethod resultMask = CalculationMethod.None;
        foreach (var method in updatedValues)
        {
            resultMask |= method;
        }

        Model.AllowedMethods = resultMask;
    }
}