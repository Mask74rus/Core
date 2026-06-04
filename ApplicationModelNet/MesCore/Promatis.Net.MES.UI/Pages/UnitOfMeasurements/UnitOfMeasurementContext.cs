using MudBlazor;
using Promatis.Net.MES.Domain;
using Promatis.Net.UI.Components;

namespace Promatis.Net.MES.UI.Pages.UnitOfMeasurements;

/// <summary>
/// Прикладной контекст управления справочником единиц измерения (Штуки, Килограммы, Метры).
/// Содержит 0 строк инфраструктурной рутины. Фокусируется только на связи со своим диалогом.
/// </summary>
public class UnitOfMeasurementContext : ReferenceContext<UnitOfMeasurement>
{
    /// <summary>
    /// Конструктор принимает только единственный IServiceProvider и коллбек обновления.
    /// Передает isInMemoryMode: true, так как справочник НСИ идеально кэшируется в ОЗУ.
    /// </summary>
    public UnitOfMeasurementContext(
        IServiceProvider serviceProvider,
        Action? onDataChangedNotifier = null)
        : base(serviceProvider, isInMemoryMode: true, onDataChangedNotifier: onDataChangedNotifier)
    {
    }

    /// <summary>
    /// Точка конкретизации визуального окна. 
    /// Связывает абстрактные CRUD-команды создания/изменения ядра с нативным generic-диалогом MudBlazor.
    /// </summary>
    /// <summary>
    /// Точка конкретизации визуального окна. 
    /// ИСПРАВЛЕНО (Честный коммит в СУБД): Ловит успешный результат закрытия диалога 
    /// и транслирует валидную модель в Брокер данных для фиксации в PostgreSQL!
    /// </summary>
    protected override async Task OpenDialogWindowAsync(UnitOfMeasurement model, bool isNew)
    {
        var parameters = new DialogParameters<UnitOfMeasurementDialog> { { "Model", model } };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Small, FullWidth = true };

        // 1. Открываем окно
        IDialogReference dialog = await DialogService.ShowAsync<UnitOfMeasurementDialog>("...", parameters, options);

        // 2. Ждем, пока BaseDialogLayout проверит форму через FluentValidation и закроет окно
        DialogResult result = await dialog.Result;

        // 3. Если окно закрылось по кнопке "Сохранить" (результат валиден)
        if (!result.Canceled && result.Data is UnitOfMeasurement validatedModel)
        {
            // 4. СТРОГО ПО НАШЕЙ АРХИТЕКТУРЕ: Вызываем базовый CRUD-сервис репозитория для коммита в PostgreSQL!
            if (isNew)
            {
                // Вызываем метод базового сервиса, который мы вытащили из DI в DataContext (Шаг 12)
                await GetBaseService().AddAsync(validatedModel);
            }
            else
            {
                await GetBaseService().UpdateAsync(validatedModel);
            }

            // 5. Оповещаем систему. Наш Брокер/Стратегия мутаций (Шаг 13) обновят ОЗУ за O(1),
            // контекст вызовет OnContextUpdated, и страница автоматически обновит грид!
            NotifyStateChanged();
            OnContextUpdated?.Invoke();
        }
    }
}