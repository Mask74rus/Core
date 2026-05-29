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

    public TechnologicalParameterPageContext(
        TechnologicalParameterService parameterService,
        IDialogService dialogService)
    {
        _parameterService = parameterService ?? throw new ArgumentNullException(nameof(parameterService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        PageTitle = "Технологические параметры";

        IsCreateVisible = true;
        IsEditVisible = true;
        IsDeleteVisible = true;

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

    public Task OnCreateActionAsync()
    {
        var newParameter = new Domain.TechnologicalParameter();

        // Прямой вызов обобщенного метода ядра с указанием конкретной Blazor-формы диалога
        return OpenEditDialogAsync<TechnologicalParameterDialog>(
            newParameter,
            "Создание технологического параметра",
            isNew: true,
            saveDelegate: async () =>
            {
                // Хирургически изолируем навигационный граф перед EF Core
                newParameter.UnitOfMeasurement = null;
                await _parameterService.AddAsync(newParameter);
            }
        );
    }

    public Task OnUpdateActionAsync()
    {
        if (SelectedItem == null) return Task.CompletedTask;

        Domain.TechnologicalParameter targetClone = CloneEntity(SelectedItem);
        targetClone.UnitOfMeasurementId = SelectedItem.UnitOfMeasurementId;
        targetClone.UnitOfMeasurement = SelectedItem.UnitOfMeasurement;

        // Прямой вызов обобщенного метода ядра для редактирования строки параметра
        return OpenEditDialogAsync<TechnologicalParameterDialog>(
            targetClone,
            $"Редактирование: {targetClone.Name}",
            isNew: false,
            saveDelegate: async () =>
            {
                targetClone.UnitOfMeasurement = null;
                await _parameterService.UpdateAsync(targetClone);
            }
        );
    }

    public async Task OnDeleteActionAsync()
    {
        if (SelectedItem == null) return;

        bool? confirm = await _dialogService.ShowMessageBoxAsync(
            "Удаление записи",
            $"Вы действительно хотите удалить технологический параметр '{SelectedItem.Name}'?",
            yesText: "Удалить", cancelText: "Отмена");

        if (confirm == true)
        {
            await _parameterService.DeleteAsync(SelectedItem.Id);
        }
    }
}