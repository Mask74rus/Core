using Promatis.Net.MES.Domain.Interface;

namespace Promatis.Net.MES.Domain;

public static class UnitHierarchyEngine
{
    /// <summary>
    /// Глобальный источник правды: проверяет, валидно ли вложение ребенка в родителя по правилам MES.
    /// Используется намертво как в СУБД-триггерах, так и в UI-валидации.
    /// </summary>
    public static bool IsHierarchyValid(UnitKind parentKind, UnitKind childKind)
    {
        // 1. Терминальный узел Position никогда не может быть родителем
        if (parentKind == UnitKind.Position) return false;

        // 2. Департамент может содержать всё, кроме Position
        if (parentKind == UnitKind.Department) return childKind != UnitKind.Position;

        // 3. Специализированные зоны изолированы друг от друга, но могут содержать себя или Position
        if (parentKind is UnitKind.Production or UnitKind.Transport or UnitKind.Storage)
        {
            return childKind == parentKind || childKind == UnitKind.Position;
        }

        return true;
    }

    /// <summary>
    /// Возвращает список ОДОБРЕННЫХ типов (UnitType) для выпадающего списка в UI,
    /// основываясь на Категории (UnitKind) родительского узла, в который мы сейчас добавляем подузел.
    /// </summary>
    public static IEnumerable<UnitType> GetAllowedChildTypes(UnitKind parentKind)
    {
        var allTypes = (UnitType[])Enum.GetValues(typeof(UnitType));
        var allowedTypes = new List<UnitType>();

        foreach (UnitType type in allTypes)
        {
            if (type == UnitType.None) continue;

            // 1. Сначала находим, к какому потенциальному Kind относится этот тип.
            // Сканируем все категории, чтобы понять, в какую маску попадает этот UnitType
            foreach (UnitKind potentialChildKind in (UnitKind[])Enum.GetValues(typeof(UnitKind)))
            {
                // Если тип побитово входит в эту категорию
                if (((int)potentialChildKind & (int)type) != 0)
                {
                    // И если правила иерархии РАЗРЕШАЮТ вложить эту категорию в текущего родителя
                    if (IsHierarchyValid(parentKind, potentialChildKind))
                    {
                        if (!allowedTypes.Contains(type))
                        {
                            allowedTypes.Add(type);
                        }
                    }
                }
            }
        }

        return allowedTypes;
    }
}