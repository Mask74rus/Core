using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Service;

namespace Promatis.Net.UI.Components.References.Dialogs;

public partial class ReferenceEditDialog<TEntity> : ComponentBase
    where TEntity : class, new()
{
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;

    [Parameter] public required TEntity Model { get; set; }
    [Parameter] public bool IsNew { get; set; } // <-- Передаем чистый bool параметр вместо лямбды

    [Parameter] public string Title { get; set; } = "Редактирование записи";

    protected ReferenceEditDialogContext<TEntity> Context { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Извлекаем из DI строго типизированные сервисы бэкенда для нашей сущности
        var referenceService = (IReferenceService<TEntity>)ServiceProvider.GetRequiredService(typeof(IReferenceService<TEntity>));
        var validator = (IValidator?)ServiceProvider.GetService(typeof(IValidator<TEntity>));

        // Инициализируем контекст диалога чистыми ссылками
        Context = new ReferenceEditDialogContext<TEntity>(Model, referenceService, IsNew, validator, Snackbar);
    }
}