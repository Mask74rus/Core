using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Promatis.Net.UI.Components.Workspaces;

public partial class UiToolbar : ComponentBase
{
    /// <summary>
    /// Список полиморфных контролов (кнопок, фильтров, селекторов) для отрисовки на панели.
    /// </summary>
    [Parameter]
    public IEnumerable<IUiControl>? Controls { get; set; }
}