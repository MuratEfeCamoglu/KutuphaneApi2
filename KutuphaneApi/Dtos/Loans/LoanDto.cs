namespace KutuphaneApi.Dtos.Loans;

public record LoanDto(
    int Id,
    int BookId,
    string BookTitle,
    int MemberId,
    string MemberFullName,
    DateTime LoanDate,
    DateTime DueDate,
    DateTime? ReturnDate);
