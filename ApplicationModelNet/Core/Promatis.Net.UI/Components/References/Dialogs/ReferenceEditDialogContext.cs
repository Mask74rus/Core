using FluentValidation;
using MudBlazor;
using Promatis.Net.Service;
using Promatis.Net.UI.Components.Dialogs;
using Promatis.Net.UI.Components.Dialogs.Buttons;
using static MudBlazor.CategoryTypes;

namespace Promatis.Net.UI.Components.References.Dialogs;

public class ReferenceEditDialogContext<TEntity> : DialogActionContextBase<TEntity>
    where TEntity : class, new()
{
    private readonly IReferenceService<TEntity> _referenceService;
    private readonly bool _isNew;

    // Внедряем сервис и флаг режима прямо в C# контекст диалога, без участия DialogParameters!
    public ReferenceEditDialogContext(
        TEntity model,
        IReferenceService<TEntity> referenceService,
        bool isNew,
        IValidator? validator,
        ISnackbar snackbar)
        : base(model, validator, onSaveAction: null, snackbar) // Передаем null в onSaveAction
    {
        _referenceService = referenceService ?? throw new ArgumentNullException(nameof(referenceService));
        _isNew = isNew;

        AddAction(new SubmitDialogButton(this));
        AddAction(new CancelDialogButton(this));
    }

    /// <summary>
    /// Переопределяем базовую логику сохранения.
    /// Теперь здесь выполняется строго детерминированный именованный метод без анонимных лямбд!
    /// </summary>
    public override async Task ExecuteSubmitAsync()
    {
        bool isFormValid = await ValidateFormAsync();
        if (!isFormValid) return;

        // Включаем спиннер
        NotifyStateChanged();

        try
        {
            // Вызываем честные методы бэкенд сервиса напрямую
            if (_isNew)
                await _referenceService.AddAsync(Model);
            else
                await _referenceService.UpdateAsync(Model);

            CloseSuccess();
        }
        catch (Exception ex)
        {
            // Обработка ошибок
            NotifyStateChanged();
            throw; // Базовый класс перехватит и покажет Snackbar
        }
    }
}