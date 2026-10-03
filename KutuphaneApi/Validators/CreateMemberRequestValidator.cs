using FluentValidation;
using KutuphaneApi.Dtos.Members;

namespace KutuphaneApi.Validators;

public class CreateMemberRequestValidator : AbstractValidator<CreateMemberRequest>
{
    public CreateMemberRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);

        // EmailAddress: "x@y" biçimini kontrol eden hazır kural.
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.").MaximumLength(200);

        // When: kural sadece koşul sağlanırsa çalışır. Telefon isteğe bağlı; verildiyse biçimi doğru olmalı.
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?\d{10,15}$").WithMessage("Telefon numarası 10-15 rakamdan oluşmalı, başta '+' olabilir.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
