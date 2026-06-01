using FluentValidation;
using MudBlazor;
using Promatis.Net.Domain.Interface;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.MES.Service;
using Promatis.Net.Test.MDM.Data;
using Promatis.Net.Test.MDM.Domain;
using Promatis.Net.Test.MDM.UI.Pages.Unit.Card;
using Promatis.Net.UI.Components.Tree;

namespace Promatis.Net.Test.MDM.UI.Pages.Unit;

public class UnitTreePageContext : TreeActionContext<UnitBase>
{
    private readonly IUnitBaseService<MdmApplicationDbContext> _unitService;
    private readonly IDialogService _dialogService;
    private readonly IValidator<UnitBase> _globalValidator;
    private readonly IEntityCloner _entityCloner;

    public UnitTreePageContext(
        IUnitBaseService<MdmApplicationDbContext> unitService,
        IDialogService dialogService,
        IValidator<UnitBase> globalValidator,
        IEntityCloner entityCloner) : base()
    {
        _unitService = unitService;
        _dialogService = dialogService;
        _globalValidator = globalValidator; // Сюда внедрится GlobalPolymorphicValidator<UnitBase>
        _entityCloner = entityCloner;

        PageTitle = "Производственная структура (MES)";
    }

    /// <summary>
    /// Реализация обязательного метода прогрева ОЗУ-кэша брокера данных.
    /// </summary>
    public override async Task InitializeInMemoryTreeAsync()
    {
        List<UnitBase> data = await _unitService.GetAllAsync();
        DataBroker.InMemoryItems = data;
    }

    // =========================================================================
    // РЕАЛИЗАЦИЯ КОРНЕВЫХ CRUD-ОПЕРАЦИЙ ДЛЯ КОМАНД ТУЛБАРА
    // =========================================================================

    public async Task OnCreateRootActionAsync()
    {
        // СЦЕНАРИЙ А: В дереве ничего не выбрано — создаем чистый КОРЕНЬ (Департамент)
        if (SelectedData == null)
        {
            var rootUnit = new DepartmentUnit
            {
                Id = Guid.NewGuid(),
                Type = UnitType.Workshop, // Для required-свойств задаем прямо в инициализаторе
                ParentId = null,
                Parent = null
            };

            await OpenEditDialogAsync(rootUnit, "Создание корневого подразделения", isNew: true);
            return;
        }

        // СЦЕНАРИЙ Б: В дереве выбран элемент — создаем соседа РЯДОМ (на том же уровне)
        UnitBase targetUnit;

        if (SelectedData.Parent != null)
        {
            // Если у выбранного элемента есть родитель в ОЗУ, просим фабрику бэкенда 
            // создать еще один дочерний шаблон для этого же родителя. 
            // Фабрика сама вернет нужный класс (например, ProductionUnit) с заполненным Type.
            targetUnit = await _unitService.CreateChildTemplateAsync(SelectedData.Parent);
            targetUnit.Parent = SelectedData.Parent; // Восстанавливаем ссылку на родителя для UI-фильтров
        }
        else
        {
            // Если выбранный элемент сам является корнем, значит его сосед — это тоже корень
            targetUnit = new DepartmentUnit
            {
                Type = UnitType.Workshop,
                ParentId = null,
                Parent = null
            };
        }

        // Генерируем свежий Guid для новой UI-сессии
        targetUnit.Id = Guid.NewGuid();

        string dialogTitle = SelectedData.Parent != null
            ? $"Создать объект на уровне '{SelectedData.Parent.Name}'"
            : "Создание корневого объекта";

        await OpenEditDialogAsync(targetUnit, dialogTitle, isNew: true);
    }

    public async Task OnCreateChildActionAsync(UnitBase? parent)
    {
        if (parent == null) return;

        UnitBase childUnit = await _unitService.CreateChildTemplateAsync(parent);
        childUnit.Id = Guid.NewGuid();

        await OpenEditDialogAsync(childUnit, $"Добавить дочерний узел в '{parent.Name}'", isNew: true);
    }

    public async Task OnUpdateActionAsync(UnitBase? node)
    {
        if (node == null) return;

        // Вызываем централизованный движок клонирования ядра платформы
        var targetClone = _entityCloner.CloneEntity(node);

        // Восстанавливаем ссылки на живую структуру ОЗУ
        targetClone.ParentId = node.ParentId;
        targetClone.Parent = node.Parent;

        await OpenEditDialogAsync(targetClone, $"Редактирование: {node.Name}", isNew: false);
    }

    public async Task OnDeleteActionAsync(UnitBase? node)
    {
        if (node == null) return;

        var parameters = new DialogParameters { ["ContentText"] = $"Удалить '{node.Name}' ({node.Kind}) и все его дочерние узлы?" };
        var options = new DialogOptions { CloseOnEscapeKey = true };

        var dialog = await _dialogService.ShowAsync<MudMessageBox>("Удаление объекта структуры", parameters, options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await _unitService.DeleteAsync(node.Id);
        }
    }

    // =========================================================================
    // ВЫЗОВ УНИВЕРСАЛЬНОГО ДИАЛОГА РЕДАКТИРОВАНИЯ И ПЕРЕДАЧА ОПЕРАЦИЙ
    // =========================================================================
    private async Task OpenEditDialogAsync(UnitBase model, string title, bool isNew)
    {
        var parameters = new DialogParameters
        {
            ["Title"] = title,
            ["IsNew"] = isNew,
            ["Model"] = model,
            ["Validator"] = _globalValidator,
            ["OnSaveAction"] = async () =>
            {
                // ХИРУРГИЧЕСКАЯ ОЧИСТКА НАВИГАЦИИ ПЕРЕД ОТПРАВКОЙ В EF CORE
                // Зануляем ссылки вверх и вниз, чтобы убрать рекурсивный обход трекера СУБД
                model.Parent = null;
                model.Children?.Clear();

                if (isNew)
                    await _unitService.AddAsync(model);
                else
                    await _unitService.UpdateAsync(model);
            }
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        await _dialogService.ShowAsync<UnitEditDialog>(title, parameters, options);
    }

    /// <summary>
    /// ДИНАМИЧЕСКИЙ РАСЧЕТ ДОСТУПНОСТИ КНОПКИ ДОБАВЛЕНИЯ ПОДУЗЛА.
    /// ИСПРАВЛЕНО: Кнопка автоматически гаснет, если выбрана терминальная рабочая точка (Position).
    /// </summary>
    public override bool IsCreateChildEnabled =>
        SelectedData != null && SelectedData.Kind != UnitKind.Position;
}