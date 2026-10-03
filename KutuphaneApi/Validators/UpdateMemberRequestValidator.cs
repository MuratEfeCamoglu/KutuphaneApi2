using FluentValidation;
using KutuphaneApi.Dtos.Members;

namespace KutuphaneApi.Validators;

public class UpdateMemberRequestValidator : AbstractValidator<UpdateMemberRequest>
{
    public UpdateMemberRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.").MaximumLength(200);
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?\d{10,15}$").WithMessage("Telefon numarası 10-15 rakamdan oluşmalı, başta '+' olabilir.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
