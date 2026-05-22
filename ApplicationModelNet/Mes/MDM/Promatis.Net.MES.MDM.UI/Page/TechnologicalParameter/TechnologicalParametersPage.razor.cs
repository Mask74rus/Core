using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Service;
using Promatis.Net.UI.Components.BaseGrid;

namespace Promatis.Net.MES.MDM.UI.Page.TechnologicalParameter;


public partial class TechnologicalParametersPage : ComponentBase
{
    [Inject] protected IReferenceService<Domain.TechnologicalParameter> ParameterService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    protected GridActionContext<Domain.TechnologicalParameter> _context = new() { PageTitle = "Технологические параметры" };
    protected List<Domain.TechnologicalParameter> _parameters = new();
    protected bool _isLoading = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await LoadDataAsync();
    }

    protected async Task LoadDataAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            _parameters = await ParameterService.GetAllAsync();
        }
        finally
        {
            _context.SelectedData = null;
            _isLoading = false;
            StateHasChanged();
        }
    }

    protected async Task CreateParameterAsync()
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var parameters = new DialogParameters<TechnologicalParameterDialog>
        {
            { x => x.Model, new Domain.TechnologicalParameter() },
            { x => x.IsNew, true }
        };

        var dialog = await DialogService.ShowAsync<TechnologicalParameterDialog>("Создание параметра", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            // Ручной вызов LoadDataAsync() УДАЛЕН — real-time обновление идет автоматически от интерцептора
            Snackbar.Add("Параметр успешно создан", Severity.Success);
        }
    }

    protected async Task EditParameterAsync(Domain.TechnologicalParameter selectedItem)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        // Генерируем чистую копию модели, изолируя изменения доменной карточки от рантайм-сессии
        var modelCopy = new Domain.TechnologicalParameter
        {
            Id = selectedItem.Id,
            Code = selectedItem.Code,
            Name = selectedItem.Name,
            DataType = selectedItem.DataType,
            UnitOfMeasurement = selectedItem.UnitOfMeasurement,
            Description = selectedItem.Description,
            CreatedAt = selectedItem.CreatedAt
        };

        var parameters = new DialogParameters<TechnologicalParameterDialog>
        {
            { x => x.Model, modelCopy },
            { x => x.IsNew, false }
        };

        var dialog = await DialogService.ShowAsync<TechnologicalParameterDialog>("Редактирование параметра", parameters, options);
        var result = await dialog.Result;

        if (result is { Canceled: false })
        {
            // Ручной вызов LoadDataAsync() УДАЛЕН — холст сам перерисует обновленную строку
            Snackbar.Add("Параметр успешно обновлен", Severity.Success);
        }
    }

    protected async Task DeleteParameterAsync(Domain.TechnologicalParameter selectedItem)
    {
        bool? confirm = await DialogService.ShowMessageBoxAsync(
            "Предупреждение",
            $"Вы действительно хотите удалить параметр '{selectedItem.Name}'?",
            yesText: "Удалить", cancelText: "Отмена");

        if (confirm == true)
        {
            try
            {
                // Отдаем команду бэкенд-сервису. Коммит в БД запустит реактивную цепочку real-time обновления
                await ParameterService.DeleteAsync(selectedItem.Id);
                Snackbar.Add("Параметр удален", Severity.Success);
            }
            catch (Exception ex)
            {
                string message = ex.InnerException?.Message ?? ex.Message;
                Snackbar.Add($"Ошибка удаления: {message}", Severity.Error);
            }
        }
    }
}