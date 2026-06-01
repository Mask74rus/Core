using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.EditDialogs;

public partial class DialogTab : ComponentBase
{
    [CascadingParameter] protected EditDialog ParentDialog { get; set; } = null!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (ParentDialog != null)
        {
            ParentDialog.RegisterTab(this); // Нативно отдаем себя наверх в коллекцию родителя
        }
    }
}