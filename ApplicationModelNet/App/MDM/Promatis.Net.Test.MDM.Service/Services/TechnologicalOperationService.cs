using Microsoft.EntityFrameworkCore;
using Promatis.Net.MES.MDM.Domain;
using Promatis.Net.MES.Service;
using Promatis.Net.Test.MDM.Data;

namespace Promatis.Net.Test.MDM.Service;

/// <summary>
/// КОНКРЕТНЫЙ (НЕ абстрактный) рабочий сервис управления технологическими операциями на самом нижнем уровне.
/// Наследует всю математику дерева и матрицы оборудования из базовых классов и реализует фабрику шаблонов.
/// </summary>
public class TechnologicalOperationService(IDbContextFactory<MdmApplicationDbContext> contextFactory)
    : TechnologicalOperationService<TechnologicalOperation, TechnologicalOperationUnit, MdmApplicationDbContext>(contextFactory)
{
    // Метод GetAllowedUnitsAsync, а также MoveAsync, GetFullTreeAsync и базовый CRUD
    // УЖЕ полностью реализованы и работают на уровнях интерфейсов и базовых классов! 
    // Писать их здесь заново НЕ НУЖНО.

    // =========================================================================
    // ПРИКЛАДНАЯ ДОМЕННАЯ ФАБРИКА ТЕХПРОЦЕССОВ
    // =========================================================================

    /// <summary>
    /// Автоматически генерирует пустой шаблон технологической операции на основе родителя.
    /// Полностью освобождает UI-слой от рутинного вызова лямбда-фабрик.
    /// </summary>
    public override Task<TechnologicalOperation> CreateChildTemplateAsync(TechnologicalOperation parent)
    {
        // По доменным правилам вашего техпроцесса создаем экземпляр конкретной операции,
        // предзаполняя её ParentId идентификатором выбранного родительского узла.
        var childOperation = new TechnologicalOperation
        {
            ParentId = parent.Id,
            IsLeaf = true // По умолчанию новая операция является листом графа, пока в неё не добавят подузлы
        };

        // Зануляем циклическую ссылку на родительский объект в памяти для защиты ChangeTracker EF Core
        childOperation.Parent = null;

        return Task.FromResult(childOperation);
    }
}