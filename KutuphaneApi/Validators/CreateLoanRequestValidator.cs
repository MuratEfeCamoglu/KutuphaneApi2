using FluentValidation;
using KutuphaneApi.Dtos.Loans;

namespace KutuphaneApi.Validators;

public class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(x => x.BookId).GreaterThan(0);
        RuleFor(x => x.MemberId).GreaterThan(0);
    }
}
