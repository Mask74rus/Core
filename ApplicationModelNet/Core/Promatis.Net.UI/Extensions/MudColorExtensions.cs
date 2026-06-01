using MudBlazor;

namespace Promatis.Net.UI;

public static class MudColorExtensions
{
    public static Color GetActionColor(this string? action)
    {
        if (string.IsNullOrWhiteSpace(action)) return Color.Default;

        return action.ToLower().Trim() switch
        {
            "create" or "insert" or "added" or "добавление" or "создание" => Color.Success,
            "update" or "edit" or "modified" or "изменение" or "редактирование" => Color.Warning,
            "delete" or "remove" or "deleted" or "softdeleted" or "удаление" => Color.Error,
            _ => Color.Default
        };
    }
}