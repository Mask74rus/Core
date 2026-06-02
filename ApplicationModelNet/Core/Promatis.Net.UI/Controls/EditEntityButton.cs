using MudBlazor;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Controls;

/// <summary>
/// Универсальный инфраструктурный элемент кнопки "Редактировать" для выбранной сущности.
/// </summary>
public class EditEntityButton<TEntity> : BaseUiControl where TEntity : class
{
    private Func<TEntity, Task>? _command;

    public override string Id => $"crud_edit_{typeof(TEntity).Name.ToLower()}";
    public override Type ComponentType => typeof(ButtonRenderBase);
    public override string Title => "Редактировать";
    public override string Icon => Icons.Material.Filled.Edit;
    public override string Tooltip => "Редактировать выбранную запись";

    public EditEntityButton<TEntity> OnExecute(Func<TEntity, Task> command)
    {
        _command = command;
        return this;
    }

    public override bool IsEnabledForData(object? targetData) => targetData is TEntity;

    protected override async Task HandleTriggerAsync(object? targetData)
    {
        if (_command != null && targetData is TEntity typedEntity)
        {
            await _command.Invoke(typedEntity);
        }
    }
}