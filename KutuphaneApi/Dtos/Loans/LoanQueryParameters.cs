using KutuphaneApi.Common.Pagination;

namespace KutuphaneApi.Dtos.Loans;

// GET /api/loans?status=Overdue&memberId=2&bookId=5&page=1&pageSize=10
public record LoanQueryParameters : PagingParameters
{
    public LoanStatus? Status { get; init; }
    public int? MemberId { get; init; }
    public int? BookId { get; init; }
}
