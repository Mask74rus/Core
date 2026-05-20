using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Promatis.Net.Domain;

public class GlobalPolymorphicValidator<T> : AbstractValidator<T> where T : class
{
    private readonly IServiceProvider _serviceProvider;

    public GlobalPolymorphicValidator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override bool PreValidate(ValidationContext<T> context, ValidationResult result)
    {
        if (context.InstanceToValidate == null) return true;

        // 1. Получаем реальный тип объекта в рантайме (например, DepartmentUnit или AuditLog)
        Type realType = context.InstanceToValidate.GetType();

        // Если тип совпадает с T (т.е. это конечный класс, а не базовый абстрактный),
        // то искать дочерний валидатор не нужно, чтобы не уйти в бесконечную рекурсию
        if (realType == typeof(T)) return true;

        // 2. Строим тип интерфейса для точечного валидатора (например, IValidator<DepartmentUnit>)
        Type specificValidatorType = typeof(IValidator<>).MakeGenericType(realType);

        // 3. Запрашиваем из DI ВСЕ валидаторы для этого конкретного типа
        // Это важно, так как для одного типа может быть зарегистрировано несколько валидаторов
        List<IValidator> specificValidators = _serviceProvider.GetServices(specificValidatorType).Cast<IValidator>().ToList();

        if (specificValidators.Any())
        {
            // Создаем не-дженерик контекст валидации FluentValidation для передачи данных
            ValidationContext<object>? newContext = ValidationContext<object>.CreateWithOptions(context.InstanceToValidate, options =>
            {
                // ИСПРАВЛЕНО: Каноничный метод FluentValidation для прямой проброски селектора свойств в стратегию
                if (context.Selector != null)
                {
                    options.UseCustomSelector(context.Selector);
                }
            });

            // Запускаем каждый найденный точечный валидатор наследника
            foreach (IValidator validator in specificValidators)
            {
                // Чтобы избежать зацикливания, проверяем, что найденный валидатор — это не сам GlobalPolymorphicValidator
                if (validator.GetType().IsGenericType && validator.GetType().GetGenericTypeDefinition() == typeof(GlobalPolymorphicValidator<>))
                    continue;

                ValidationResult specificResult = validator.Validate(newContext);

                // Переносим все обнаруженные ошибки в общий результат валидации ядра
                foreach (ValidationFailure error in specificResult.Errors)
                {
                    result.Errors.Add(error);
                }
            }
        }

        return true;
    }
}