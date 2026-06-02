using FluentValidation;
using MudBlazor;
using Severity = MudBlazor.Severity;

namespace Promatis.Net.UI.Components.Dialogs;

public abstract class DialogActionContextBase<TModel> : IDialogActionContext
    where TModel : class, new()
{
    private readonly List<IUiControl> _actions = [];
    private readonly IValidator? _validator;
    private readonly Func<Task>? _onSaveAction;
    private readonly ISnackbar _snackbar;

    private MudForm? _form;
    private IMudDialogInstance? _dialogInstance;
    private bool _isProcessing;

    public TModel Model { get; }
    public object ModelObject => Model;
    public bool IsProcessing => _isProcessing;
    public IReadOnlyCollection<IUiControl> Actions => _actions.AsReadOnly();

    public event Action? OnContextStateChanged;

    protected DialogActionContextBase(TModel model, IValidator? validator, Func<Task>? onSaveAction, ISnackbar snackbar)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        _validator = validator;
        _onSaveAction = onSaveAction;
        _snackbar = snackbar ?? throw new ArgumentNullException(nameof(snackbar));
    }

    public void BindForm(MudForm form) => _form = form;
    public void BindDialogInstance(IMudDialogInstance dialogInstance) => _dialogInstance = dialogInstance;
    public void AddAction(IUiControl action) => _actions.Add(action);
    public void NotifyStateChanged() => OnContextStateChanged?.Invoke();

    public async Task<bool> ValidateFormAsync()
    {
        if (_form == null) return true;

        await _form.ValidateAsync();
        if (!_form.IsValid)
        {
            _snackbar.Add("Пожалуйста, исправьте ошибки в форме перед сохранением.", Severity.Warning);
            return false;
        }

        return true;
    }

    /// <summary>
    /// НАША РЕАЛИЗАЦИЯ ДЛЯ ИНТЕРФЕЙСА: Срабатывает при клике на SubmitDialogButton
    /// </summary>
    public async Task ExecuteSubmitAsync()
    {
        // 1. Запускаем валидацию всей формы (включая FluentValidation через MudForm)
        bool isFormValid = await ValidateFormAsync();
        if (!isFormValid) return;

        // 2. Включаем режим обработки (блокирует кнопки от повторных кликов)
        _isProcessing = true;
        NotifyStateChanged();

        try
        {
            // 3. Вызываем реальный метод сохранения в базу данных (gRPC/API)
            if (_onSaveAction != null)
            {
                await _onSaveAction.Invoke();
            }

            // 4. Закрываем диалог с успешным результатом
            CloseSuccess();
        }
        catch (Exception ex)
        {
            string message = ex.InnerException?.Message ?? ex.Message;
            _snackbar.Add($"Ошибка сохранения: {message}", Severity.Error);
        }
        finally
        {
            // 5. В любом случае снимаем блокировку и выключаем спиннер
            _isProcessing = false;
            NotifyStateChanged();
        }
    }

    public void CloseSuccess() => _dialogInstance?.Close(DialogResult.Ok(Model));
    public void CloseCancel() => _dialogInstance?.Cancel();

    public async Task<IEnumerable<string>> ExecuteFluentValidationAsync(object model)
    {
        if (_validator == null) return [];
        var context = new ValidationContext<object>(model);
        var result = await _validator.ValidateAsync(context);
        return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
    }
}