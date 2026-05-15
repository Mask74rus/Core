namespace Promatis.Net.Domain.Interface;

/// <summary>
/// Интерфейс приведения типов родителя и потомков в дереве
/// </summary>
/// <typeparam name="T"></typeparam>
public interface ITreeNode<T> where T : class
{
    T? Parent { get; }

    ICollection<T> Children { get; }
}