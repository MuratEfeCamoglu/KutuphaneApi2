using FluentValidation;
using KutuphaneApi.Dtos.Books;

namespace KutuphaneApi.Validators;

public class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);

        // Matches: düzenli ifade (regex) ile biçim kontrolü. ^\d{13}$ → tam olarak 13 rakam.
        RuleFor(x => x.Isbn)
            .NotEmpty()
            .Matches(@"^\d{13}$").WithMessage("ISBN tam olarak 13 rakamdan oluşmalıdır.");

        RuleFor(x => x.PublishedYear)
            .InclusiveBetween(1, timeProvider.GetUtcNow().Year);

        RuleFor(x => x.StockCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AuthorId).GreaterThan(0);

        // RuleForEach: koleksiyonun her elemanına aynı kuralı uygular.
        RuleFor(x => x.CategoryIds).NotNull();
        RuleForEach(x => x.CategoryIds).GreaterThan(0);
    }
}
