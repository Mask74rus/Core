using Promatis.Net.Configuration;

namespace Promatis.Net.ApplicationModel.Console;

public class ConsoleBootstrapper : AppBootstrapper
{
    protected override IAppConfigurator CreateConfigurator() => new ConsoleConfigurator();
}