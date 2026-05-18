namespace Promatis.Net.UI;

public interface IGridReferenceViewModel : IGridViewModel
{
    string Name { get; }
    string? Code { get; }
}