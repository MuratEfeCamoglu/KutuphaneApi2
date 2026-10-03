using KutuphaneApi.Dtos.Loans;
using KutuphaneApi.Entities;

namespace KutuphaneApi.Mappings;

public static class LoanMappings
{
    public static IQueryable<LoanDto> SelectDto(this IQueryable<Loan> query) =>
        query.Select(l => new LoanDto(
            l.Id,
            l.BookId,
            l.Book.Title,
            l.MemberId,
            l.Member.FirstName + " " + l.Member.LastName,
            l.LoanDate,
            l.DueDate,
            l.ReturnDate));
}
