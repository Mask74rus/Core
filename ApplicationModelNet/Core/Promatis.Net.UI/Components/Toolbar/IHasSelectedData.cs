namespace Promatis.Net.UI.Components.Toolbar;

/// <summary>
/// Универсальный интерфейс-маркер для визуализаторов, которым необходимо 
/// синхронизировать фокус строки/узла и получать импульсы перерисовки.
/// </summary>
public interface IHasSelectedData<TEntity> where TEntity : class
{
    TEntity? SelectedData { get; set; }
    Action? OnContextUpdated { get; set; }
}