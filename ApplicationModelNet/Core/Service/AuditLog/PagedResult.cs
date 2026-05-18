namespace Promatis.Net.Service;

public record PagedResult<T>(IReadOnlyCollection<T> Items, int TotalCount);