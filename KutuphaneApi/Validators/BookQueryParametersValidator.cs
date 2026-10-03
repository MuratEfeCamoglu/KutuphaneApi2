using FluentValidation;
using KutuphaneApi.Dtos.Books;

namespace KutuphaneApi.Validators;

public class BookQueryParametersValidator : AbstractValidator<BookQueryParameters>
{
    private static readonly string[] SortFields = ["title", "year"];
    private static readonly string[] SortDirections = ["asc", "desc"];

    public BookQueryParametersValidator()
    {
        // Include: başka bir validator'ın kurallarını bu validator'a ekler (page, pageSize).
        Include(new PagingParametersValidator());

        RuleFor(x => x.SortBy)
            .Must(value => value is null || SortFields.Contains(value.ToLowerInvariant()))
            .WithMessage("sortBy 'title' veya 'year' olmalıdır.");

        RuleFor(x => x.SortDirection)
            .Must(value => value is null || SortDirections.Contains(value.ToLowerInvariant()))
            .WithMessage("sortDirection 'asc' veya 'desc' olmalıdır.");
    }
}
