using FluentValidation;
using Microsoft.AspNetCore.Components;
using Promatis.Net.MES.Domain;
using Promatis.Net.MES.Domain.Interface;
using Promatis.Net.Test.MDM.Domain;

namespace Promatis.Net.Test.MDM.UI.Pages.Unit.Card;

public partial class UnitEditDialog : ComponentBase
{
    [Parameter] public required string Title { get; set; }
    [Parameter] public required bool IsNew { get; set; }
    [Parameter] public required UnitBase Model { get; set; }
    [Parameter] public required FluentValidation.IValidator Validator { get; set; }
    [Parameter] public required Func<Task> OnSaveAction { get; set; }

    /// <summary>
    /// Возвращает список разрешенных Категорий (UnitKind) для выпадающего списка в UI,
    /// основываясь строго на правилах UnitHierarchyEngine относительно родительского узла.
    /// </summary>
    private IEnumerable<UnitKind> GetAllowedUnitKinds()
    {
        if (Model.Parent == null)
        {
            return Enum.GetValues(typeof(UnitKind)).Cast<UnitKind>();
        }

        // Нативно опрашиваем движок домена: какие категории разрешено вложить в родителя
        UnitKind parentKind = Model.Parent.Kind;
        return Enum.GetValues(typeof(UnitKind))
            .Cast<UnitKind>()
            .Where(childKind => UnitHierarchyEngine.IsHierarchyValid(parentKind, childKind));
    }

    /// <summary>
    /// Возвращает список типов, отфильтрованных по битовой маске текущей выбранной категории
    /// </summary>
    private IEnumerable<UnitType> GetRenderedUnitTypes()
    {
        return Enum.GetValues(typeof(UnitType))
            .Cast<UnitType>()
            .Where(type => type != UnitType.None && ((int)Model.Kind & (int)type) != 0);
    }

    /// <summary>
    /// Перехватывает смену категории пользователем в выпадающем списке.
    /// На лету пересоздает правильный C#-класс наследника СУБД для обхода init-only ограничений,
    /// полностью сохраняя уже введенный пользователем текст в полях формы.
    /// </summary>
    private void OnUnitKindChanged(UnitKind newKind)
    {
        if (Model.Kind == newKind) return;

        // 1. Сначала вычисляем первый доступный дефолтный тип для новой маски категории
        UnitType defaultType = Enum.GetValues(typeof(UnitType))
            .Cast<UnitType>()
            .FirstOrDefault(type => type != UnitType.None && ((int)newKind & (int)type) != 0);

        // 2. ИСПРАВЛЕНО: Передаем defaultType строго в инициализаторы объектов при их создании
        UnitBase newModel = newKind switch
        {
            UnitKind.Department => new DepartmentUnit { Type = defaultType },
            UnitKind.Production => new ProductionUnit { Type = defaultType },
            UnitKind.Storage => new StorageUnit { Type = defaultType },
            UnitKind.Transport => new TransportUnit { Type = defaultType },
            UnitKind.Position => new PositionUnit { Type = defaultType },
            _ => throw new ArgumentOutOfRangeException(nameof(newKind))
        };

        // 3. Переносим накопленное текстовое состояние формы в новый полиморфный объект
        newModel.Id = Model.Id;
        newModel.Code = Model.Code;
        newModel.Name = Model.Name;
        newModel.Description = Model.Description;
        newModel.ParentId = Model.ParentId;
        newModel.Parent = Model.Parent;

        // Подменяем модель в диалоге — Blazor мгновенно перерисует форму и реактивно обновит вкладки
        Model = newModel;
        StateHasChanged();
    }
}