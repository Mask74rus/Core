using Microsoft.AspNetCore.Components;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.MDM.Domain;
using Promatis.Net.UI.Components.Tree;

namespace Promatis.Net.MES.MDM.UI.Pages.TechnologicalOperations.Card;

public partial class TechnologicalOperationEditDialog : ComponentBase
{
    [CascadingParameter] protected TreeActionContext<TechnologicalOperation> ActionContext { get; set; } = null!;

    [Parameter] public required string Title { get; set; }
    [Parameter] public required bool IsNew { get; set; }
    [Parameter] public required TechnologicalOperation Model { get; set; }
    [Parameter] public required FluentValidation.IValidator Validator { get; set; }
    [Parameter] public required Func<Task> OnSaveAction { get; set; }

    /// <summary>
    /// Вычисляет, нужно ли заблокировать изменение флага IsLeaf в UI для защиты графа.
    /// </summary>
    protected bool IsLeafModificationDisabled()
    {
        // При создании нового элемента менять статус можно свободно
        if (IsNew) return false;

        // При редактировании опрашиваем ОЗУ-кэш брокера данных: если внутри этой папки 
        // уже физически лежат дочерние операции — блокируем переключатель намертво!
        if (ActionContext?.DataBroker?.InMemoryItems != null)
        {
            bool hasChildrenInCache = ActionContext.DataBroker.InMemoryItems
                .Any(x => x.ParentId == Model.Id);

            // Если дети есть — превратить в лист (IsLeaf = true) запрещено доменным движком
            return !TechnologicalOperationHierarchyEngine.CanChangeLeafStatus(hasChildrenInCache, requestedIsLeaf: true);
        }

        return false;
    }
}