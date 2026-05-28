using FluentValidation;
using MudBlazor;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Service;
using Promatis.Net.MES.UI.Pages.UnitOfMeasurements.Card;
using Promatis.Net.UI.Components.Grid;

namespace Promatis.Net.MES.UI.Pages.UnitOfMeasurements;

public class UnitOfMeasurementPageContext : GridActionContext<UnitOfMeasurement>
{
    private readonly IUnitOfMeasurementService _uomService;
    private readonly IDialogService _dialogService;
    private readonly IValidator<UnitOfMeasurement> _globalValidator;

    public UnitOfMeasurementPageContext(
        IUnitOfMeasurementService uomService,
        IDialogService dialogService,
        IValidator<UnitOfMeasurement> globalValidator) : base()
    {
        _uomService = uomService ?? throw new ArgumentNullException(nameof(uomService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _globalValidator = globalValidator ?? throw new ArgumentNullException(nameof(globalValidator));

        PageTitle = "Справочник единиц измерения";
    }

    /// <summary>
    /// Чистый прикладной метод загрузки данных из СУБД в ОЗУ-кэш для привязки к MudDataGrid
    /// </summary>
    public async Task<GridData<UnitOfMeasurement>> GetUnitsOfMeasurementAsync()
    {
        List<UnitOfMeasurement> data = await _uomService.GetAllAsync();
        return new GridData<UnitOfMeasurement>
        {
            Items = data,
            TotalItems = data.Count
        };
    }

    // =========================================================================
    // КОМАНДЫ ПАНЕЛИ ИНСТРУМЕНТОВ (CRUD)
    // =========================================================================

    public async Task OnCreateActionAsync()
    {
        var newUom = new UnitOfMeasurement();
        await OpenEditDialogAsync(newUom, "Добавление единицы измерения", isNew: true);
    }

    public async Task OnUpdateActionAsync(UnitOfMeasurement row)
    {
        if (row == null) return;
        var targetClone = CloneEntity(row);
        await OpenEditDialogAsync(targetClone, $"Редактирование: {row.Code}", isNew: false);
    }

    public async Task OnDeleteActionAsync(UnitOfMeasurement row)
    {
        if (row == null) return;

        var parameters = new DialogParameters { ["ContentText"] = $"Удалить единицу измерения '{row.Name}' ({row.Code})?" };
        var options = new DialogOptions { CloseOnEscapeKey = true };

        var dialog = await _dialogService.ShowAsync<MudMessageBox>("Удаление объекта", parameters, options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await _uomService.DeleteAsync(row.Id);
            RequestRefresh();
        }
    }

    private async Task OpenEditDialogAsync(UnitOfMeasurement model, string title, bool isNew)
    {
        var parameters = new DialogParameters
        {
            ["Title"] = title,
            ["IsNew"] = isNew,
            ["Model"] = model,
            ["Validator"] = _globalValidator,
            ["OnSaveAction"] = async () =>
            {
                if (isNew)
                    await _uomService.AddAsync(model);
                else
                    await _uomService.UpdateAsync(model);
            }
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        await _dialogService.ShowAsync<UnitOfMeasurementEditDialog>(title, parameters, options);
    }
}