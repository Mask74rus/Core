using MudBlazor;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Service;
using Promatis.Net.MES.UI.Pages.UnitOfMeasurements.Card;
using Promatis.Net.UI.Components.Grid;

namespace Promatis.Net.MES.UI.Pages.UnitOfMeasurements;

public class UnitOfMeasurementPageContext : GridActionContext<UnitOfMeasurement>
{
    private readonly IUnitOfMeasurementService _uomService;
    private readonly IDialogService _dialogService;
    private readonly IEntityCloner _entityCloner;

    public UnitOfMeasurementPageContext(
        IUnitOfMeasurementService uomService,
        IDialogService dialogService,
        IEntityCloner entityCloner)
    {
        _uomService = uomService ?? throw new ArgumentNullException(nameof(uomService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _entityCloner = entityCloner ?? throw new ArgumentNullException(nameof(entityCloner));

        PageTitle = "Справочник единиц измерения";

        // Включаем нативный ОЗУ-режим для привязки к GridPage
        DataBroker.ConfigureInMemoryMode();
    }

    /// <summary>
    /// ТОЧЕЧНОЕ ДОБАВЛЕНИЕ: Метод первичного наполнения ОЗУ-кэша брокера данных
    /// </summary>
    public async Task InitializeInMemoryDataAsync()
    {
        try
        {
            List<UnitOfMeasurement> data = await _uomService.GetAllAsync();
            DataBroker.InMemoryItems = data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Ошибка инициализации ОЗУ-кэша единиц измерения: {ex.Message}");
        }
    }

    // =========================================================================
    // КОМАНДЫ ПАНЕЛИ ИНСТРУМЕНТОВ (CRUD)
    // =========================================================================

    public async Task OnCreateActionAsync()
    {
        var newUom = new UnitOfMeasurement();
        await OpenEditDialogAsync<UnitOfMeasurementEditDialog>(
            newUom,
            "Добавление единицы измерения",
            isNew: true,
            saveDelegate: () => _uomService.AddAsync(newUom)
        );
    }

    public async Task OnUpdateActionAsync(UnitOfMeasurement? row)
    {
        if (row == null) return;
        UnitOfMeasurement targetClone = _entityCloner.CloneEntity(row);
        await OpenEditDialogAsync<UnitOfMeasurementEditDialog>(
            targetClone,
            $"Редактирование: {row.Code}",
            isNew: false,
            saveDelegate: () => _uomService.UpdateAsync(targetClone)
        );
    }

    public async Task OnDeleteActionAsync()
    {
        if (SelectedItem == null) return;

        bool? confirm = await _dialogService.ShowMessageBoxAsync(
            "Удаление записи",
            $"Вы действительно хотите удалить единицу измерения '{SelectedItem.Name}' ({SelectedItem.Code})?",
            yesText: "Удалить", cancelText: "Отмена");

        if (confirm == true)
        {
            await _uomService.DeleteAsync(SelectedItem.Id);
        }
    }

}