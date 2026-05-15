using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.MDM.Domain;

public class TechnologicalOperation : TechnologicalOperationBase<
    TechnologicalOperation,
    TechnologicalOperationUnit,
    TechnologicalOperationParameter>
{
    // Класс абсолютно чист. 
    // Он автоматически содержит свойства Parent и Children с типом TechnologicalOperation.
}