using FluentValidation;
using KutuphaneApi.Dtos.Loans;

namespace KutuphaneApi.Validators;

public class LoanQueryParametersValidator : AbstractValidator<LoanQueryParameters>
{
    public LoanQueryParametersValidator()
    {
        Include(new PagingParametersValidator());

        // IsInEnum: ?status=7 gibi enum'da karşılığı olmayan sayıları reddeder.
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.MemberId).GreaterThan(0).When(x => x.MemberId is not null);
        RuleFor(x => x.BookId).GreaterThan(0).When(x => x.BookId is not null);
    }
}
