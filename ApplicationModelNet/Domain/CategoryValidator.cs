using FluentValidation;

namespace Promatis.Net.Domain;

public class CategoryValidator : DomainObjectValidator<Category> {
    public CategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class Category : ReferenceTreeBase<Category>
{

    public string Name { get; set; }
}
