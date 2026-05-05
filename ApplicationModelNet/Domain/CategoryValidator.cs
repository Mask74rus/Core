using FluentValidation;

namespace Promatis.Net.Domain;

public class CategoryValidator : ReferenceTreeBaseValidator<Category> {
    public CategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class Category : ReferenceTreeBase<Category>
{

    public string Name { get; set; }
}
