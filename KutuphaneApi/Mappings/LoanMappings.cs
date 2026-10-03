using KutuphaneApi.Dtos.Loans;
using KutuphaneApi.Entities;

namespace KutuphaneApi.Mappings;

public static class LoanMappings
{
    // Durum hesabı "şu an"a bağlı olduğu için now parametre olarak alınır.
    // Üçlü (?:) ifade SQL'de CASE WHEN ... THEN ... END'e çevrilir; hesap veritabanında yapılır.
    public static IQueryable<LoanDto> SelectDto(this IQueryable<Loan> query, DateTime now) =>
        query.Select(l => new LoanDto(
            l.Id,
            l.BookId,
            l.Book.Title,
            l.MemberId,
            l.Member.FirstName + " " + l.Member.LastName,
            l.LoanDate,
            l.DueDate,
            l.ReturnDate,
            l.ReturnDate != null
                ? LoanStatus.Returned
                : l.DueDate < now ? LoanStatus.Overdue : LoanStatus.Active));

    // Durum filtresi: SelectDto'daki hesapla birebir aynı koşullar.
    public static IQueryable<Loan> WhereStatus(this IQueryable<Loan> query, LoanStatus status, DateTime now) =>
        status switch
        {
            LoanStatus.Returned => query.Where(l => l.ReturnDate != null),
            LoanStatus.Overdue => query.Where(l => l.ReturnDate == null && l.DueDate < now),
            _ => query.Where(l => l.ReturnDate == null && l.DueDate >= now)
        };
}
