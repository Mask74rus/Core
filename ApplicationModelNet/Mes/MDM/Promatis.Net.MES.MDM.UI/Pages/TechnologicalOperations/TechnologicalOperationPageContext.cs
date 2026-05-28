using FluentValidation;

using MudBlazor;
using Promatis.Net.MES.MDM.Domain;
using Promatis.Net.MES.MDM.UI.Pages.TechnologicalOperations.Card;
using Promatis.Net.MES.Service;

using Promatis.Net.UI.Components.Tree;

namespace Promatis.Net.MES.MDM.UI.Pages.TechnologicalOperations;

public class TechnologicalOperationPageContext : TreeActionContext<TechnologicalOperation>
{
    private readonly ITechnologicalOperationService<TechnologicalOperation, TechnologicalOperationUnit> _operationService;
    private readonly IDialogService _dialogService;
    private readonly IValidator<TechnologicalOperation> _globalValidator;

    /// <summary>
    /// ДИНАМИЧЕСКИЙ РАСЧЕТ ДОСТУПНОСТИ КНОПКИ ДОБАВЛЕНИЯ ПОДУЗЛА.
    /// Нативно опрашивает доменный движок: если выбранная операция является Листом (IsLeaf = true),
    /// кнопка "Добавить подузел" автоматически гаснет на тулбаре UI, защищая СУБД от ошибок.
    /// </summary>
    public override bool IsCreateChildEnabled =>
        SelectedData != null && !SelectedData.IsLeaf;

    public TechnologicalOperationPageContext(
        ITechnologicalOperationService<TechnologicalOperation, TechnologicalOperationUnit> operationService,
        IDialogService dialogService,
        IValidator<TechnologicalOperation> globalValidator) : base()
    {
        _operationService = operationService ?? throw new ArgumentNullException(nameof(operationService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _globalValidator = globalValidator ?? throw new ArgumentNullException(nameof(globalValidator));

        PageTitle = "Справочник технологических операций";
    }

    /// <summary>
    /// Реализация обязательного метода прогрева ОЗУ-кэша брокера данных.
    /// Вызывается автоматически при инициализации компонента TreePage без блокировки UI.
    /// </summary>
    public override async Task InitializeInMemoryTreeAsync()
    {
        List<TechnologicalOperation> data = await _operationService.GetAllAsync();
        DataBroker.InMemoryItems = data;
    }

    // =========================================================================
    // РЕАЛИЗАЦИЯ CRUD-ОПЕРАЦИЙ ДЛЯ КОМАНД ТУЛБАРА (ПАТТЕРН СОСЕДА И ПОДУЗЛА)
    // =========================================================================

    /// <summary>
    /// Вызывается тулбаром при нажатии кнопки "Создать" (Новый корень или сосед на текущем уровне).
    /// </summary>
    public async Task OnCreateRootOrSiblingActionAsync()
    {
        TechnologicalOperation targetUnit;

        // СЦЕНАРИЙ А: В дереве ничего не выбрано — создаем чистый КОРЕНЬ (Группировочную папку)
        if (SelectedData == null)
        {
            targetUnit = new TechnologicalOperation
            {
                Id = Guid.NewGuid(),
                IsLeaf = false, // По умолчанию корень — это папка-группа
                ParentId = null,
                Parent = null,
                Code = string.Empty,
                Name = string.Empty,
                Description = string.Empty
            };

            await OpenEditDialogAsync(targetUnit, "Создание корневой группы операций", isNew: true);
            return;
        }

        // СЦЕНАРИЙ Б: В дереве выбран элемент — создаем соседа РЯДОМ (на том же уровне родителя)
        if (SelectedData.Parent != null)
        {
            // Просим фабрику бэкенда создать чистый шаблон дочернего элемента для текущего родителя
            targetUnit = await _operationService.CreateChildTemplateAsync(SelectedData.Parent);
            targetUnit.Parent = SelectedData.Parent; // Восстанавливаем ссылку на родителя для UI-фильтров
        }
        else
        {
            // Если выбранный элемент сам корень, значит его сосед — это тоже корень
            targetUnit = new TechnologicalOperation
            {
                IsLeaf = false,
                ParentId = null,
                Parent = null,
                Code = string.Empty,
                Name = string.Empty,
                Description = string.Empty
            };
        }

        targetUnit.Id = Guid.NewGuid();

        string dialogTitle = SelectedData.Parent != null
            ? $"Создать операцию на уровне '{SelectedData.Parent.Name}'"
            : "Создание корневой операции";

        await OpenEditDialogAsync(targetUnit, dialogTitle, isNew: true);
    }

    /// <summary>
    /// Вызывается тулбаром при нажатии кнопки "Добавить подузел".
    /// </summary>
    public async Task OnCreateChildActionAsync(TechnologicalOperation parent)
    {
        if (parent == null) return;

        // Запрашиваем предзаполненный шаблон у фабрики сервисного слоя бэкенда
        TechnologicalOperation childUnit = await _operationService.CreateChildTemplateAsync(parent);
        childUnit.Id = Guid.NewGuid();
        childUnit.Parent = parent; // Принудительно связываем в ОЗУ для контекста диалога

        await OpenEditDialogAsync(childUnit, $"Добавить подузел в '{parent.Name}'", isNew: true);
    }

    /// <summary>
    /// Вызывается тулбаром при нажатии кнопки "Изменить".
    /// </summary>
    public async Task OnUpdateActionAsync(TechnologicalOperation node)
    {
        if (node == null) return;

        // Вызываем централизованный движок глубокого клонирования ядра платформы (из WorkspaceActionContext)
        var targetClone = CloneEntity(node);

        // Восстанавливаем навигационные указатели на живое дерево в оперативной памяти
        targetClone.ParentId = node.ParentId;
        targetClone.Parent = node.Parent;

        await OpenEditDialogAsync(targetClone, editTitle: $"Редактирование: {node.Name}", isNew: false);
    }

    /// <summary>
    /// Вызывается тулбаром при нажатии кнопки "Удалить".
    /// </summary>
    public async Task OnDeleteActionAsync(TechnologicalOperation node)
    {
        if (node == null) return;

        // Если у папки есть вложенные элементы в ОЗУ-кэше, выводим предупреждение
        bool hasChildren = DataBroker.InMemoryItems?.Any(x => x.ParentId == node.Id) ?? false;
        string confirmationMessage = hasChildren
            ? $"Внимание! Группа '{node.Name}' содержит вложенные операции. Удалить её вместе со всеми подузлами?"
            : $"Вы уверены, что хотите удалить технологическую операцию '{node.Name}'?";

        var parameters = new DialogParameters { ["ContentText"] = confirmationMessage };
        var options = new DialogOptions { CloseOnEscapeKey = true };

        var dialog = await _dialogService.ShowAsync<MudMessageBox>("Удаление операции", parameters, options);
        var result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await _operationService.DeleteAsync(node.Id);
        }
    }

    // =========================================================================
    // ВЫЗОВ УНИВЕРСАЛЬНОГО ДИАЛОГА ФОРМЫ
    // =========================================================================
    private async Task OpenEditDialogAsync(TechnologicalOperation model, string editTitle, bool isNew)
    {
        var parameters = new DialogParameters
        {
            ["Title"] = editTitle,
            ["IsNew"] = isNew,
            ["Model"] = model,
            ["Validator"] = _globalValidator,
            ["OnSaveAction"] = async () =>
            {
                if (isNew)
                {
                    // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Намертво зануляем навигационную ссылку на родителя в ОЗУ.
                    // Это блокирует рекурсивный обход EF Core и полностью ликвидирует ошибку 23505 (Unique Violation).
                    // База данных свяжет ветки по плоскому полю ParentId, которое заполнено идеально.
                    model.Parent = null;

                    await _operationService.AddAsync(model);
                }
                else
                {
                    // Для обновления навигационную ссылку тоже зануляем, чтобы избежать каскадных конфликтов трекинга
                    model.Parent = null;

                    await _operationService.UpdateAsync(model);
                }
            }
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        await _dialogService.ShowAsync<TechnologicalOperationEditDialog>(editTitle, parameters, options);
    }
}