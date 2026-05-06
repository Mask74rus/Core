namespace Promatis.Net.Data;

public interface IUserProvider
{
    // Метод может быть асинхронным, если получение имени требует запроса
    Task<string?> GetCurrentUserNameAsync();
}