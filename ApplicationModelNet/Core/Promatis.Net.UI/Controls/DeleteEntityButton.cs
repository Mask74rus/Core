using MudBlazor;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Controls;

/// <summary>
/// Универсальный инфраструктурный элемент кнопки "Удалить" для выбранной сущности.
/// </summary>
public class DeleteEntityButton<TEntity> : BaseUiControl where TEntity : class
{
    private Func<TEntity, Task>? _command;

    public override string Id => $"crud_delete_{typeof(TEntity).Name.ToLower()}";
    public override Type ComponentType => typeof(ButtonRenderBase);
    public override string Title => "Удалить";
    public override string Icon => Icons.Material.Filled.Delete;
    public override string Tooltip => "Удалить выбранную запись";

    public DeleteEntityButton<TEntity> OnExecute(Func<TEntity, Task> command)
    {
        _command = command;
        return this;
    }

    public override bool IsEnabledForData(object? targetData)
    {
        if (targetData is not TEntity typedEntity) return false;
        if (typedEntity is Domain.Interface.ISoftDeletable softDeletable)
        {
            return softDeletable.DeletedAt == null;
        }
        return true;
    }

    protected override async Task HandleTriggerAsync(object? targetData)
    {
        if (_command != null && targetData is TEntity typedEntity)
        {
            await _command.Invoke(typedEntity);
        }
    }
}