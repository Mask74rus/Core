using FluentValidation;
using Promatis.Net.Domain;

namespace Promatis.Net.MES.Tests.Operation;

public class TestOperationValidator : ReferenceTreeBaseValidator<TestOperation>
{
    public TestOperationValidator()
    {
        // Правило: Группа операций (IsLeaf == false) не может содержать прямые связи с оборудованием
        RuleFor(x => x.UnitLinks)
            .Must(links => links == null || links.Count == 0)
            .When(x => !x.IsLeaf)
            .WithMessage("Группа операций не может содержать прямые связи с оборудованием.");
    }
}