using System.Reflection;
using Promatis.Net.Domain.Interface;

namespace Promatis.Net.MES.Domain;

public static class TechnologicalOperationHierarchyEngine
{
    /// <summary>
    /// Глобальный источник правды: проверяет, валидно ли вложение дочерней операции в родительскую.
    /// Намертво блокирует создание веток внутри терминальных операций (IsLeaf = true).
    /// Типизация переведена на чистый контракт дерева T, что полностью устраняет дженерик-коллизии C#.
    /// </summary>
    /// <typeparam name="T">Конкретный тип технологической операции</typeparam>
    /// <param name="parent">Родительская операция</param>
    /// <param name="childIsLeaf">Признак того, является ли создаваемый/перемещаемый ребенок листом</param>
    public static bool IsHierarchyValid<T>(T? parent, bool childIsLeaf)
        where T : class, ITreeNode<T>
    {
        // 1. Если родителя нет, мы создаем корневой элемент — это всегда валидно по правилам СУБД
        if (parent == null)
        {
            return true;
        }

        // 2. Через рефлексию (или каст к интерфейсу, если бы он был) извлекаем признак IsLeaf родителя.
        // Так как T наследуется от TechnologicalOperationBase, свойство IsLeaf там гарантированно присутствует.
        PropertyInfo? isLeafProperty = typeof(T).GetProperty("IsLeaf");
        if (isLeafProperty != null)
        {
            bool parentIsLeaf = (bool)isLeafProperty.GetValue(parent)!;
            if (parentIsLeaf)
            {
                return false; // Терминальный узел не может быть родителем
            }
        }

        return true;
    }

    /// <summary>
    /// Проверяет, разрешено ли переключать флаг IsLeaf у существующей операции.
    /// Если у операции уже физически есть дочерние ветки в ОЗУ, превращать её в лист запрещено.
    /// </summary>
    public static bool CanChangeLeafStatus(bool hasChildren, bool requestedIsLeaf)
    {
        if (hasChildren && requestedIsLeaf)
        {
            return false;
        }

        return true;
    }
}