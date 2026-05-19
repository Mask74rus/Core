using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Promatis.Net.Configuration.Web;

public class ComponentRegistry
{
    private readonly Dictionary<string, Type> _routeMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Сканирует сборки модулей и строит карту: URL -> Сlass Тип компонента
    /// </summary>
    public void RegisterModules(IEnumerable<Assembly> assemblies)
    {
        foreach (Assembly assembly in assemblies)
        {
            IEnumerable<Type> componentTypes = assembly.GetTypes()
                .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract);

            foreach (Type type in componentTypes)
            {
                // Ищем атрибут [RouteAttribute], в который компилируется директива @page
                IEnumerable<RouteAttribute> routeAttributes = type.GetCustomAttributes<RouteAttribute>();
                foreach (RouteAttribute attr in routeAttributes)
                {
                    _routeMap[attr.Template] = type;
                }
            }
        }
    }

    public Type? GetComponentTypeByRoute(string route)
    {
        return _routeMap.TryGetValue(route, out Type? type) ? type : null;
    }
}