using FluentValidation;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Promatis.Net.MES.MDM.Service;
using Promatis.Net.MES.MDM.UI.Page.TechnologicalParameter.Card;
using Promatis.Net.UI.Components.EditDialog;
using Promatis.Net.UI.Components.Grid;

namespace Promatis.Net.MES.MDM.UI.Page.TechnologicalParameter;

/// <summary>
/// Контекст управления прикладной страницей справочника технологических параметров.
/// </summary>
public class TechnologicalParameterPageContext : GridActionContext<Domain.TechnologicalParameter>
{
    private readonly TechnologicalParameterService _parameterService;
    private readonly IDialogService _dialogService;
    private readonly IValidator _globalValidator;

    // .NET 10 DI рантайм автоматически внедрит сюда службу, менеджер диалогов MudBlazor и полиморфный валидатор
    public TechnologicalParameterPageContext(
        TechnologicalParameterService parameterService,
        IDialogService dialogService,
        IValidator<Domain.TechnologicalParameter> globalValidator) : base()
    {
        _parameterService = parameterService;
        _dialogService = dialogService;
        _globalValidator = globalValidator;

        PageTitle = "Технологические параметры";

        // Включаем базовые кнопки CRUD на тулбаре
        IsCreateVisible = true;
        IsEditVisible = true;
        IsDeleteVisible = true;

        // Запускаем асинхронную предзагрузку данных для ОЗУ-режима
        _ = InitializeInMemoryDataAsync();
    }

    private async Task InitializeInMemoryDataAsync()
    {
        try
        {
            List<Domain.TechnologicalParameter> allParameters = await _parameterService.GetAllAsync();
            DataBroker.ConfigureInMemoryMode(allParameters);
            RequestRefresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Ошибка инициализации ОЗУ-справочника параметров: {ex.Message}");
        }
    }

    // =========================================================================
    // CRUD-АВТОМАТИКА ТУЛБАРНЫХ КНОПОК ПО СОГЛАШЕНИЮ ОБ ИМЕНАХ
    // =========================================================================

    /// <summary>
    /// Автоматически вызывается при нажатии кнопки «Создать» на UiToolbar
    /// </summary>
    public async Task OpenCreateDialogAsync()
    {
        var newParameter = new Domain.TechnologicalParameter();

        var dialogOptions = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialogParameters = new DialogParameters<EditDialog>
        {
            { x => x.Title, "Создание технологического параметра" },
            { x => x.IsNew, true },
            { x => x.Model, newParameter },
            { x => x.Validator, _globalValidator },
            { x => x.OnSaveAction, async () => await _parameterService.AddAsync(newParameter) },
            { x => x.Tabs, BuildDialogTabs(newParameter) } // Строим и передаем коллекцию вкладок
        };

        var dialog = await _dialogService.ShowAsync<EditDialog>("", dialogParameters, dialogOptions);
        var result = await dialog.Result;

        // ОЗУ-мутация произойдет автоматически через HandleGlobalEntityCommit от импульса СУБД
    }

    /// <summary>
    /// Автоматически вызывается при нажатии кнопки «Изменить» на UiToolbar
    /// </summary>
    public async Task OpenEditDialogAsync()
    {
        if (SelectedData == null) return;

        // Передаем клон или сам объект, если ваши интерцепторы СУБД отслеживают изменения сущностей
        var parameterToEdit = SelectedData;

        var dialogOptions = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
        var dialogParameters = new DialogParameters<EditDialog>
        {
            { x => x.Title, $"Редактирование: {parameterToEdit.Name}" },
            { x => x.IsNew, false },
            { x => x.Model, parameterToEdit },
            { x => x.Validator, _globalValidator },
            { x => x.OnSaveAction, async () => await _parameterService.UpdateAsync(parameterToEdit) },
            { x => x.Tabs, BuildDialogTabs(parameterToEdit) }
        };

        await _dialogService.ShowAsync<EditDialog>("", dialogParameters, dialogOptions);
    }

    /// <summary>
    /// Автоматически вызывается при нажатии кнопки «Удалить» на UiToolbar
    /// </summary>
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

    /// <summary>
    /// Внутренняя фабрика сборки вкладок. Передает контент через RenderFragment
    /// </summary>
    private List<DialogTab> BuildDialogTabs(Domain.TechnologicalParameter parameter)
    {
        return new List<DialogTab>
        {
            new()
            {
                Title = "Основное",
                Icon = Icons.Material.Filled.Info,
                Content = (RenderFragment)(builder =>
                {
                    builder.OpenComponent<TechnologicalParameterGeneralTab>(0);
                    builder.AddAttribute(1, "Model", parameter);
                    builder.CloseComponent();
                })
            },
            new()
            {
                Title = "Характеристики",
                Icon = Icons.Material.Filled.Build,
                Content = (RenderFragment)(builder =>
                {
                    builder.OpenComponent<TechnologicalParameterTechnicalTab>(0);
                    builder.AddAttribute(1, "Model", parameter);
                    builder.CloseComponent();
                })
            }
        };
    }

    public override void HandleGlobalEntityCommit(object state, object entity)
    {
        if (entity is Domain.TechnologicalParameter)
        {
            RequestRefresh();
        }
    }
}