using MudBlazor;

namespace Promatis.Net.Configuration.Web;

/// <summary>
/// Шина UI-событий
/// </summary>
public class UiCommandBus : IUiCommandBus
{
    // Потокобезопасный словарь для регистрации команд в рантайме SignalR
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Func<Dictionary<string, object>, Task<DialogResult?>>> _handlers = new();

    public void RegisterHandler(string commandName, Func<Dictionary<string, object>, Task<DialogResult?>> handler)
    {
        _handlers[commandName] = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public async Task<DialogResult?> ExecuteAsync(string commandName, Dictionary<string, object> parameters)
    {
        if (_handlers.TryGetValue(commandName, out Func<Dictionary<string, object>, Task<DialogResult?>>? handler))
        {
            return await handler(parameters);
        }

        // Если модуль (например, DCA) не подключен в лаунчере, система не упадет, а мягко уведомит
        return null;
    }
}