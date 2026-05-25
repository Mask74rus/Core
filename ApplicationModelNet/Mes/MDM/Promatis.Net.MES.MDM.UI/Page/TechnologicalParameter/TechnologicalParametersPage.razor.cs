using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Data;
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
        if (firstRender)
        {
            // ПЕРВОНАЧАЛЬНАЯ ЗАГРУЗКА: PostgreSQL опрашивается строго 1 раз за сессию
            await LoadDataAsync();
        }
    }

    protected async Task LoadDataAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            _parameters = await ParameterService.GetAllAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Ошибка загрузки параметров: {ex.Message}", Severity.Error);
        }
        finally
        {
            _context.SelectedData = null;
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Перехватчик real-time импульсов СУБД. Нам не нужно делать повторный SELECT в базу,
    /// так как базовый BaseGridPage уже хирургически обновил коллекцию _parameters в ОЗУ.
    /// Метод просто синхронизирует состояние кнопок тулбара и обновляет графику.
    /// </summary>
    protected Task HandleIncrementalUpdateAsync((EntityStateChangeEnum State, Domain.TechnologicalParameter Entity) delta)
    {
        // Пересчитываем стейты кнопок тулбара (Изменить/Удалить) на основе новых данных в памяти
        _context.SelectedData = _context.SelectedData;

        StateHasChanged();
        return Task.CompletedTask;
    }

    // =========================================================================
    // CRUD ОПЕРАЦИИ И ВЫЗОВЫ ДОМЕННЫХ КАРТОЧЕК
    // =========================================================================

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
            Snackbar.Add("Параметр успешно создан", Severity.Success);
        }
    }

    protected async Task EditParameterAsync(Domain.TechnologicalParameter selectedItem)
    {
        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        // Изолируем доменную карточку от живой сессии таблицы, создавая чистую копию
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
                // Удаление в БД запустит каскадный интерцептор, и строка исчезнет из ОЗУ за 0 мс
                await ParameterService.DeleteAsync(selectedItem.Id);
                Snackbar.Add("Параметр успешно удален", Severity.Success);
            }
            catch (Exception ex)
            {
                string message = ex.InnerException?.Message ?? ex.Message;
                Snackbar.Add($"Ошибка удаления: {message}", Severity.Error);
            }
        }
    }
}