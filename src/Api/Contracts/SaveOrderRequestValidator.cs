using FluentValidation;

namespace Api.Contracts;

public sealed class SaveOrderLineRequestValidator : AbstractValidator<SaveOrderLineRequest>
{
    public SaveOrderLineRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Артикул товару обов'язковий.")
            .MaximumLength(50).WithMessage("Артикул товару — до 50 символів.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Назва товару обов'язкова.")
            .MaximumLength(200).WithMessage("Назва товару — до 200 символів.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0m).WithMessage("Ціна не може бути від'ємною.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Кількість має бути більшою за нуль.");
    }
}

public sealed class SaveOrderRequestValidator : AbstractValidator<SaveOrderRequest>
{
    public SaveOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Ідентифікатор клієнта обов'язковий.")
            .MaximumLength(50).WithMessage("Ідентифікатор клієнта — до 50 символів.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Замовлення має містити хоча б один рядок.");

        RuleForEach(x => x.Lines)
            .NotNull().WithMessage("Рядок замовлення не може бути порожнім.");

        RuleForEach(x => x.Lines)
            .SetValidator(new SaveOrderLineRequestValidator());
    }
}
