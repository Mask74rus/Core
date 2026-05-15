using Promatis.Net.UI;

namespace Promatis.Net.Configuration.Web;

// Сервис, который собирает всё воедино
public class UiModuleService(IEnumerable<IUiModule> modules)
{
    public IEnumerable<IUiModule> Modules => modules;
}