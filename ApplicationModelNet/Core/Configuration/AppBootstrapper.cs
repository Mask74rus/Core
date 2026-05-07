using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Runtime.Loader;

namespace Promatis.Net.Configuration;

public abstract class AppBootstrapper
{
    public virtual void Run(string[] args)
    {
        // 1. ПРИНУДИТЕЛЬНАЯ ЗАГРУЗКА DLL (чтобы Type.GetType их видел)
        LoadProjectAssemblies("Promatis.");

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        // 2. УМНЫЙ ПОИСК КОНФИГУРАТОРОВ ИЗ JSON
        List<IAppConfigurator> configurators = GetConfigurators(builder.Configuration).ToList();

        foreach (IAppConfigurator config in configurators)
        {
            config.ConfigureServices(builder.Services, builder.Configuration);
        }

        IHost host = builder.Build();

        foreach (IAppConfigurator config in configurators)
        {
            config.ConfigureApp(host);
        }

        host.Run();
    }

    protected virtual IEnumerable<IAppConfigurator> GetConfigurators(IConfiguration configuration)
    {
        string[] typeNames = configuration.GetSection("LauncherSettings:Modules").Get<string[]>()
                             ?? [];

        foreach (string name in typeNames)
        {
            // Ищем тип во всех загруженных сборках
            Type? type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(name))
                .FirstOrDefault(t => t != null);

            if (type != null && typeof(IAppConfigurator).IsAssignableFrom(type))
            {
                if (Activator.CreateInstance(type) is IAppConfigurator instance)
                {
                    yield return instance;
                }
            }
            else
            {
                Console.WriteLine($"[BOOTSTRAPPER][WARN] Тип не найден или не валиден: {name}");
            }
        }
    }

    private void LoadProjectAssemblies(string prefix)
    {
        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (path == null) return;

        foreach (string file in Directory.GetFiles(path, $"{prefix}*.dll"))
        {
            try
            {
                AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
            }
            catch { /* Игнорируем ошибки загрузки */ }
        }
    }
}