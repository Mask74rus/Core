using MudBlazor;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Controls;

/// <summary>
/// Универсальный инфраструктурный элемент кнопки "Удалить" для выбранной сущности.
/// </summary>
public class DeleteEntityButton<TEntity> : BaseUiControl where TEntity : class
{
    public override string Id => $"crud_delete_{typeof(TEntity).Name.ToLower()}";
    public override Type ComponentType => typeof(ButtonRenderBase);
    public override string Title => "Удалить";
    public override string Icon => Icons.Material.Filled.Delete;
    public override string Tooltip => "Удалить выбранную запись";

    public override bool IsEnabledForData(object? targetData)
    {
        if (targetData is not TEntity typedEntity) return false;

        if (typedEntity is Promatis.Net.Domain.Interface.ISoftDeletable softDeletable)
        {
            return softDeletable.DeletedAt == null;
        }

        return true;
    }

    protected override Task HandleTriggerAsync(object? targetData)
    {
        if (targetData is TEntity typedEntity)
        {
            // UI-заглушка для будущей отправки команды удаления в API
        }
        return Task.CompletedTask;
    }
}