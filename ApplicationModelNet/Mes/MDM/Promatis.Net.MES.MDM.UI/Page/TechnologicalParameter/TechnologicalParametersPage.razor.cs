using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.Service;
using Promatis.Net.UI;

namespace Promatis.Net.MES.MDM.UI.Page.TechnologicalParameter;

public partial class TechnologicalParametersPage : ComponentBase
{
    [Inject] protected IReferenceService<Domain.TechnologicalParameter> ParameterService { get; set; } = null!;
    [Inject] protected IDialogService DialogService { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    protected GridActionContext _context = new() { PageTitle = "Технологические параметры" };
    protected List<Domain.TechnologicalParameter> _parameters = new();
    protected Domain.TechnologicalParameter? _selectedParameter;
    protected MudDataGrid<Domain.TechnologicalParameter> _grid = null!;
    protected bool _isLoading = true;

    protected override void OnInitialized()
    {
        UpdateToolbarState();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
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
        finally
        {
            _selectedParameter = null;
            _isLoading = false;
            UpdateToolbarState();
            StateHasChanged();
        }
    }

    protected void OnRowClick(DataGridRowClickEventArgs<Domain.TechnologicalParameter> args)
    {
        _selectedParameter = args.Item;
        UpdateToolbarState();
    }

    protected void UpdateToolbarState()
    {
        // Кнопки "Изменить" и "Удалить" активируются только при наличии выбранной строки
        bool hasSelection = _selectedParameter != null;

        _context.IsEditEnabled = hasSelection;
        _context.IsDeleteEnabled = hasSelection;

        _context.NotifyUpdate();
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
            await LoadDataAsync();
            Snackbar.Add("Параметр успешно создан", Severity.Success);
        }
    }

    protected async Task EditParameterAsync()
    {
        if (_selectedParameter == null) return;

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        // Клонируем выделенный объект для изоляции редактирования
        var modelCopy = new Domain.TechnologicalParameter
        {
            Id = _selectedParameter.Id,
            Code = _selectedParameter.Code,
            Name = _selectedParameter.Name,
            DataType = _selectedParameter.DataType,
            UnitOfMeasurement = _selectedParameter.UnitOfMeasurement,
            Description = _selectedParameter.Description,
            CreatedAt = _selectedParameter.CreatedAt
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
            await LoadDataAsync();
            Snackbar.Add("Параметр успешно обновлен", Severity.Success);
        }
    }

    protected async Task DeleteParameterAsync()
    {
        if (_selectedParameter == null) return;

        bool? confirm = await DialogService.ShowMessageBoxAsync(
            "Предупреждение",
            $"Вы действительно хотите удалить параметр '{_selectedParameter.Name}'?",
            yesText: "Удалить", cancelText: "Отмена");

        if (confirm == true)
        {
            try
            {
                await ParameterService.DeleteAsync(_selectedParameter.Id);
                await LoadDataAsync();
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