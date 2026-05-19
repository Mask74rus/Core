using MudBlazor;

namespace Promatis.Net.Configuration.Web;

/// <summary>
/// Интерфейс шины UI-событий
/// </summary>
public interface IUiCommandBus
{
    /// <summary>
    /// Регистрация обработчика вызова формы (выполняется принимающим модулем, например DCA)
    /// </summary>
    void RegisterHandler(string commandName, Func<Dictionary<string, object>, Task<DialogResult?>> handler);

    /// <summary>
    /// Взаимодействие между модулями вслепую (вызывается из MDM)
    /// </summary>
    Task<DialogResult?> ExecuteAsync(string commandName, Dictionary<string, object> parameters);
}