using MudBlazor;
using Promatis.Net.Domain;
using Promatis.Net.Domain.Interface;
using Promatis.Net.Service;
using Promatis.Net.UI.Components.References.Dialogs;
using Promatis.Net.UI.Controls;

namespace Promatis.Net.UI.Components.References;

public class ReferenceWorkspaceContext<TEntity> : CrudWorkspaceContext<TEntity, Guid>
    where TEntity : class, IDomainObjectHasKey<Guid>, new()
{
    private readonly IReferenceService<TEntity> _referenceService;
    private readonly IDialogService _dialogService;

    // Внедряем IDialogService в конструктор универсального контекста
    public ReferenceWorkspaceContext(IReferenceService<TEntity> referenceService, IDialogService dialogService, Action onStateChangedNotifier)
        : base(referenceService, isInMemoryMode: true)
    {
        _referenceService = referenceService ?? throw new ArgumentNullException(nameof(referenceService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

        Broker = new UiDataBroker<TEntity, GridState<TEntity>, GridData<TEntity>>(onStateChangedNotifier);
        Broker.ConfigureInMemoryMode(OzuCache, EvaluateGridStateInMemory);
    }

    /// <summary>
    /// Переопределяем инициализацию кнопок, чтобы связать их с нативной службой диалогов MudBlazor
    /// </summary>
    protected override void InitializeToolbarControls()
    {
        // 1. Привязываем открытие пустого диалога создания
        AddControl(new CreateEntityButton<TEntity>().OnExecute(async () =>
            await OpenEditDialogAsync(new TEntity(), isNew: true)));

        // 2. Привязываем открытие диалога с клонированием выбранной в гриде строки
        AddControl(new EditEntityButton<TEntity>().OnExecute(async (selectedRow) =>
        {
            var cloner = new JsonEntityCloner();
            var clone = cloner.CloneEntity(selectedRow);
            await OpenEditDialogAsync(clone, isNew: false);
        }));

        // 3. Привязываем вызов MessageBox и команду удаления в СУБД
        AddControl(new DeleteEntityButton<TEntity>().OnExecute(async (selectedRow) =>
        {
            bool? confirm = await _dialogService.ShowMessageBoxAsync("Удаление", $"Вы уверены, что хотите удалить запись?", yesText: "Удалить", noText: "Отмена");
            if (confirm == true)
            {
                await _referenceService.DeleteAsync(selectedRow.Id);
            }
        }));

        AddControl(new ToolbarDivider());
    }

    /// <summary>
    /// Универсальный метод вызова модального окна для любого справочника
    /// </summary>
    private async Task OpenEditDialogAsync(TEntity model, bool isNew)
    {
        var parameters = new DialogParameters<ReferenceEditDialog<TEntity>>
        {
            { x => x.Model, model },
            { x => x.IsNew, isNew }
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        // ИСПРАВЛЕНО: Убран InvokeAsync. Прямой вызов ShowAsync нативно безопасен в Blazor
        var dialog = await _dialogService.ShowAsync<ReferenceEditDialog<TEntity>>(
            isNew ? "Создание новой записи" : "Редактирование записи",
            parameters,
            options);

        await dialog.Result;
    }
}