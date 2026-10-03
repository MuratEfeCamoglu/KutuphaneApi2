using FluentValidation;
using KutuphaneApi.Dtos.Authors;

namespace KutuphaneApi.Validators;

public class UpdateAuthorRequestValidator : AbstractValidator<UpdateAuthorRequest>
{
    public UpdateAuthorRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BirthYear)
            .Must(year => year is null || (year > 0 && year <= timeProvider.GetUtcNow().Year))
            .WithMessage("Doğum yılı 1 ile içinde bulunulan yıl arasında olmalıdır.");
    }
}
