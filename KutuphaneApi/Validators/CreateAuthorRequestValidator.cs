using FluentValidation;
using KutuphaneApi.Dtos.Authors;

namespace KutuphaneApi.Validators;

// AbstractValidator<T>: T tipindeki isteğin kurallarını okunabilir bir zincirle tanımlar.
// DI'a otomatik kaydedilir (AddValidatorsFromAssembly), servis IValidator<CreateAuthorRequest> olarak alır.
public class CreateAuthorRequestValidator : AbstractValidator<CreateAuthorRequest>
{
    public CreateAuthorRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        // Must + lambda: hazır kural yoksa kendi koşulumuzu yazarız. Doğum yılı gelecekte olamaz.
        RuleFor(x => x.BirthYear)
            .Must(year => year is null || (year > 0 && year <= timeProvider.GetUtcNow().Year))
            .WithMessage("Doğum yılı 1 ile içinde bulunulan yıl arasında olmalıdır.");
    }
}
