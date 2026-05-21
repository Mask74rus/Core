using Promatis.Net.MES.Domain;

namespace Promatis.Net.MES.MDM.Domain;

/// <summary>
/// Конкретная реализация метода расчета технологического параметра.
/// Связывает конкретную операцию, оборудование и параметр из контекста MesMDM.
/// </summary>
public class TechnologicalParameterCalcMethod : TechnologicalParameterCalcMethodBase<UnitBase, TechnologicalOperation, TechnologicalParameter>
{
    // Класс чист и готов к маппингу в DbContext модуля MesMDM.
    // Свойства Unit, TechnologicalOperation и TechnologicalParameter 
    // автоматически получили строгие конечные типы.
}