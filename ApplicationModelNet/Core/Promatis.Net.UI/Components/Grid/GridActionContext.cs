using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Promatis.Net.Configuration;
using Promatis.Net.UI.Components.Toolbar;

namespace Promatis.Net.UI.Components.Grid;

/// <summary>
/// Базовый табличный контекст управления платформы.
/// Автоматически обеспечивает высокопроизводительные ОЗУ-мутации при любых транзакциях в СУБД.
/// </summary>
public abstract class GridActionContext<TEntity> : ToolbarActionContext<TEntity> where TEntity : class
{
    // Чистое нативное извлечение служб из контейнера текущей Blazor-сессии вкладки пользователя
    protected IDialogService DialogService => ScopedProvider.GetRequiredService<IDialogService>();
    protected IValidator<TEntity> GlobalValidator => ScopedProvider.GetRequiredService<IValidator<TEntity>>();

    public TEntity? SelectedItem
    {
        get => SelectedData;
        set => SelectedData = value;
    }

    /// <summary>
    /// Универсальный брокер данных текущей таблицы.
    /// </summary>
    public UiDataBroker<TEntity> DataBroker { get; } = new();

    protected GridActionContext() => Position = ToolbarPosition.Top;


    // ПЛАТФОРМЕННЫЙ АВТОМАТИЧЕСКИЙ ДВИЖОК ОЗУ-МУТАЦИЙ 

    /// <summary>
    /// Глобальный перехватчик коммитов СУБД на уровне табличного ядра.
    /// Полностью освобождает прикладных разработчиков от ручного написания логики обновлений.
    /// </summary>
    public override void HandleGlobalEntityCommit(object? state, object? entity)
    {
        if (entity == null) return;

        Type entityType = entity.GetType();

        // Срезаем динамические прокси Castle/EF Core
        if (entityType.BaseType != null && entityType.Namespace == "Castle.Proxies")
        {
            entityType = entityType.BaseType;
        }

        // 1. Если транзакция из базы данных затронула именно тип данных нашей таблицы TEntity
        if (typeof(TEntity).IsAssignableFrom(entityType))
        {
            string stateStr = state?.ToString() ?? string.Empty;

            // 2. Автоматически пинаем ОЗУ-движок брокера применить дельту за 0 мс
            DataBroker.ApplyIncrementalOzuDelta(stateStr, (TEntity)entity);

            // 3. Вызываем базовые правила сброса фокуса ToolbarActionContext
            base.HandleGlobalEntityCommit(state, entity);

            // 4. Автоматически пинаем GridPage перерисовать HTML-строки из обновленного кэша
            RequestRefresh();
        }
    }

    protected override void RecalculateButtonStates()
    {
        base.RecalculateButtonStates();
        NotifyUpdate();
    }

    protected async Task OpenEditDialogAsync<TDialog>(TEntity model, string title, bool isNew, Func<Task> saveDelegate)
    where TDialog : Microsoft.AspNetCore.Components.IComponent
    {
        var parameters = new DialogParameters
        {
            ["Title"] = title,
            ["IsNew"] = isNew,
            ["Model"] = model,
            ["Validator"] = GlobalValidator, 
            ["OnSaveAction"] = async () =>
            {
                // Выполняем переданное прикладное действие сохранения (Add или Update)
                await saveDelegate();

                // Централизованно командуем гриду обновить данные в ОЗУ
                RequestRefresh();
            }
        };

        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        // Вызываем MudBlazor диалог для абстрактного компонента TDialog
        await DialogService.ShowAsync<TDialog>(title, parameters, options);
    }
}