using FluentValidation;
using KutuphaneApi.Common.Pagination;

namespace KutuphaneApi.Validators;

// Ortak sayfalama kuralları. Diğer validator'lar bunu Include ile kendi kurallarına ekler.
public class PagingParametersValidator : AbstractValidator<PagingParameters>
{
    public PagingParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingParameters.MaxPageSize);
    }
}
