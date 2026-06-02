using MudBlazor;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Controls;

/// <summary>
/// Универсальный инфраструктурный элемент кнопки "Создать" для сущностей любого типа.
/// </summary>
public class CreateEntityButton<TEntity> : BaseUiControl where TEntity : class, new()
{
    private Func<Task>? _command; // Наш исполнитель диалога

    public override string Id => $"crud_create_{typeof(TEntity).Name.ToLower()}";
    public override Type ComponentType => typeof(ButtonRenderBase);
    public override string Title => "Создать";
    public override string Icon => Icons.Material.Filled.Add;
    public override string Tooltip => $"Создать новую запись";

    public CreateEntityButton() { IsEnabled = true; }

    // Метод для внедрения реального действия из контекста
    public CreateEntityButton<TEntity> OnExecute(Func<Task> command)
    {
        _command = command;
        return this;
    }

    public override bool IsEnabledForData(object? targetData) => true;

    protected override async Task HandleTriggerAsync(object? targetData)
    {
        if (_command != null) await _command.Invoke();
    }
}