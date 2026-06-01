using MudBlazor;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Controls;

/// <summary>
/// Универсальный инфраструктурный элемент кнопки "Создать" для сущностей любого типа.
/// </summary>
public class CreateEntityButton<TEntity> : BaseUiControl where TEntity : class, new()
{
    public override string Id => $"crud_create_{typeof(TEntity).Name.ToLower()}";
    public override Type ComponentType => typeof(ButtonRenderBase); // Привязка к рендереру кнопок
    public override string Title => "Создать";
    public override string Icon => Icons.Material.Filled.Add;
    public override string Tooltip => $"Создать новую запись ({typeof(TEntity).Name})";

    public CreateEntityButton()
    {
        IsEnabled = true;
    }

    public override bool IsEnabledForData(object? targetData) => true;

    protected override Task HandleTriggerAsync(object? targetData)
    {
        // UI-заглушка: в будущем здесь будет открытие диалога EditDialog
        return Task.CompletedTask;
    }
}