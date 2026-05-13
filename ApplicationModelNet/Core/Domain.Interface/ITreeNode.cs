namespace Promatis.Net.Domain.Interface;

public interface ITreeNode<out T> where T : class
{
    T? TypedParent { get; }
    IEnumerable<T> TypedChildren { get; }
}