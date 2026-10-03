using FluentValidation;
using KutuphaneApi.Dtos.Books;

namespace KutuphaneApi.Validators;

public class UpdateBookRequestValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Isbn)
            .NotEmpty()
            .Matches(@"^\d{13}$").WithMessage("ISBN tam olarak 13 rakamdan oluşmalıdır.");
        RuleFor(x => x.PublishedYear)
            .InclusiveBetween(1, timeProvider.GetUtcNow().Year);
        RuleFor(x => x.StockCount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AuthorId).GreaterThan(0);
        RuleFor(x => x.CategoryIds).NotNull();
        RuleForEach(x => x.CategoryIds).GreaterThan(0);
    }
}
