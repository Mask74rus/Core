using Promatis.Net.Domain;

namespace Promatis.Net.Service;

public interface ICategoryService : IReferenceService<Category>
{
    // Здесь можно добавить специфичные методы, например:
    // Task<List<Category>> GetRootCategoriesAsync();
}