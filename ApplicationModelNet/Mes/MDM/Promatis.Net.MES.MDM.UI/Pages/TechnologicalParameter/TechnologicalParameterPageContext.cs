using FluentValidation;
using MudBlazor;
using Promatis.Net.MES.MDM.Service;
using Promatis.Net.MES.MDM.UI.Pages.TechnologicalParameter.Card;
using Promatis.Net.UI.Components.Grid;

namespace Promatis.Net.MES.MDM.UI.Pages.TechnologicalParameter;

/// <summary>
/// Контекст управления прикладной страницей справочника технологических параметров.
/// Полностью освобожден от визуального boilerplate-кода и управляет сугубо логикой рантайма.
/// </summary>
public class TechnologicalParameterPageContext : GridActionContext<Domain.TechnologicalParameter>
{
    private readonly TechnologicalParameterService _parameterService;
    private readonly IDialogService _dialogService;
    private readonly IValidator _globalValidator;

    public TechnologicalParameterPageContext(
        TechnologicalParameterService parameterService,
        IDialogService dialogService,
        IValidator<Domain.TechnologicalParameter> globalValidator) : base()
    {
        _parameterService = parameterService;
        _dialogService = dialogService;
        _globalValidator = globalValidator;

        PageTitle = "Технологические параметры";

        IsCreateVisible = true;
        IsEditVisible = true;
        IsDeleteVisible = true;

        // Просто включаем ОЗУ-режим для текущего экрана
        DataBroker.ConfigureInMemoryMode();
    }

    public async Task InitializeInMemoryDataAsync()
    {
        try
        {
            List<Domain.TechnologicalParameter> allParameters = await _parameterService.GetAllAsync();
            DataBroker.InMemoryItems = allParameters;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Ошибка инициализации ОЗУ-справочника параметров: {ex.Message}");
        }
    }

    public Task OpenCreateDialogAsync()
    {
        var newParameter = new Domain.TechnologicalParameter();
        return OpenDialogInternalAsync("Создание технологического параметра", true, newParameter, () => _parameterService.AddAsync(newParameter));
    }

    public Task OpenEditDialogAsync()
    {
        if (SelectedData == null) return Task.CompletedTask;
        return OpenDialogInternalAsync($"Редактирование: {SelectedData.Name}", false, SelectedData, () => _parameterService.UpdateAsync(SelectedData));
    }

    public async Task OpenDeleteDialogAsync()
    {
        if (SelectedData == null) return;

        bool? confirm = await _dialogService.ShowMessageBoxAsync(
            "Удаление записи",
            $"Вы действительно хотите удалить технологический параметр '{SelectedData.Name}'?",
            yesText: "Удалить", cancelText: "Отмена");

        if (confirm == true)
        {
            await _parameterService.DeleteAsync(SelectedData.Id);
        }
    }

    private async Task OpenDialogInternalAsync(string title, bool isNew, Domain.TechnologicalParameter model, Func<Task> onSave)
    {
        var dialogOptions = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialogParameters = new DialogParameters<TechnologicalParameterDialog>
        {
            { x => x.Title, title },
            { x => x.IsNew, isNew },
            { x => x.Model, model },
            { x => x.Validator, _globalValidator },
            { x => x.OnSaveAction, onSave }
        };

        await _dialogService.ShowAsync<TechnologicalParameterDialog>("", dialogParameters, dialogOptions);
    }
}