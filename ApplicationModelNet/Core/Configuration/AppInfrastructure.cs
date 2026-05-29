namespace Promatis.Net.Configuration;

/// <summary>
/// Глобальная системная точка доступа к инфраструктурным сервисам платформы Promatis.Net.
/// Инициализируется однократно при старте приложения и исключает раздувание конструкторов.
/// </summary>
public static class AppInfrastructure
{
    private static IServiceProvider? _provider;
    private static readonly object _lock = new();

    /// <summary>
    /// Read-only доступ к глобальному контейнеру зависимостей.
    /// </summary>
    public static IServiceProvider Provider
    {
        get
        {
            if (_provider == null)
            {
                throw new InvalidOperationException(
                    "Критическая ошибка: AppInfrastructure не инициализирован. " +
                    "Убедитесь, что метод AppInfrastructure.Initialize() вызван в Program.cs или WebAppBootstrapper.");
            }
            return _provider;
        }
    }

    /// <summary>
    /// Потокобезопасная инициализация локатора. Вызывается строго один раз при старте хоста.
    /// </summary>
    public static void Initialize(IServiceProvider provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        if (_provider != null) return;

        lock (_lock)
        {
            _provider ??= provider;
        }
    }
}