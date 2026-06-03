using MudBlazor;
using Promatis.Net.UI.Components;
using Promatis.Net.UI.Components.ElementRenderBase;

namespace Promatis.Net.UI.Controls;

/// <summary>
/// Специализированная кнопка-команда для создания дочернего (подчиненного) узла дерева.
/// Нативно управляет своим состоянием доступности на основе наличия выбранного родительского элемента.
/// </summary>
public class CreateChildButton<TEntity> : BaseUiControl where TEntity : class
{
    private Func<Task>? _executeAsync;
    private readonly string _id = "toolbar_action_create_child_" + Guid.NewGuid().ToString("N");

    public override string Id => _id;
    public override Type ComponentType => typeof(ButtonRenderBase); // Используем стандартный рендерер кнопок ядра
    public override string Title => "Добавить подузел";
    public override string Icon => Icons.Material.Filled.PlaylistAdd; // Иконка добавления в список / поддерево
    public override string Tooltip => "Создать дочерний элемент внутри выбранного узла";

    public CreateChildButton()
    {
        // По умолчанию кнопка заблокирована, пока ядро не передаст в нее выбранную строку
        IsEnabled = false;

        ComponentParameters.Add("Color", Color.Primary);
        ComponentParameters.Add("Variant", Variant.Outlined); // Выделяем визуально от главной кнопки создания корня
    }

    /// <summary>
    /// Реактивная проверка доступности команды силами ядра. 
    /// Метод вызывается контекстом EntityWorkspaceContext при каждом изменении SelectedData.
    /// </summary>
    public override bool IsEnabledForData(object? targetData)
    {
        // Кнопка становится доступной ТОЛЬКО тогда, когда в системе выбран родительский элемент
        bool hasParentSelected = targetData != null;

        // Синхронизируем внутреннее состояние флага доступности для рендерера MudBlazor
        IsEnabled = hasParentSelected;

        return hasParentSelected;
    }

    /// <summary>
    /// Привязка асинхронного обработчика (делегата) вызова команды из контекста страницы.
    /// </summary>
    public CreateChildButton<TEntity> OnExecute(Func<Task> executeAsync)
    {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        return this;
    }

    /// <summary>
    /// Точка входа клика по кнопке MudBlazor, вызываемая через визуальный рендерер ядра.
    /// </summary>
    protected override async Task HandleTriggerAsync(object? targetData)
    {
        if (_executeAsync != null && IsEnabled)
        {
            await _executeAsync.Invoke();
        }
    }
}