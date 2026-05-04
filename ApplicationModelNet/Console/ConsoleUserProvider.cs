using Promatis.Net.Data;

namespace Promatis.Net.ApplicationModel.Console;

public class ConsoleUserProvider : IUserProvider
{
    public Task<string?> GetCurrentUserNameAsync()
        => Task.FromResult<string?>(Environment.UserName);
}