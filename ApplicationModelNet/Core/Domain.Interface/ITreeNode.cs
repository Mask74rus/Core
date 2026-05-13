namespace Promatis.Net.Domain.Interface;

/// <summary>
/// Интерфейс приведения типов родителя и потомков в дереве
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ITreeNode<out T> where T : class
{
    T? TypedParent { get; }

    IEnumerable<T> TypedChildren { get; }
}