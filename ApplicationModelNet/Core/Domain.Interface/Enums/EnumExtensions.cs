using System.ComponentModel;
using System.Reflection;

namespace Promatis.Net.Domain.Interface;

public static class EnumExtensions
{
    /// <summary>
    /// Возвращает значение атрибута [Description] для любого элемента перечисления.
    /// Если атрибут отсутствует, возвращает стандартное имя .ToString().
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();

        var attribute = field.GetCustomAttribute<DescriptionAttribute>();
        return attribute != null ? attribute.Description : value.ToString();
    }
}