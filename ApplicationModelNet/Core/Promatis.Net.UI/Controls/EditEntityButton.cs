using MudBlazor;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Controls;

/// <summary>
/// Универсальный инфраструктурный элемент кнопки "Редактировать" для выбранной сущности.
/// </summary>
public class EditEntityButton<TEntity> : BaseUiControl where TEntity : class
{
    public override string Id => $"crud_edit_{typeof(TEntity).Name.ToLower()}";
    public override Type ComponentType => typeof(ButtonRenderBase);
    public override string Title => "Редактировать";
    public override string Icon => Icons.Material.Filled.Edit;
    public override string Tooltip => "Редактировать выбранную запись";

    public override bool IsEnabledForData(object? targetData)
    {
        return targetData is TEntity;
    }

    protected override Task HandleTriggerAsync(object? targetData)
    {
        if (targetData is TEntity typedEntity)
        {
            // UI-заглушка для будущего открытия EditDialog с данными сущности
        }
        return Task.CompletedTask;
    }
}