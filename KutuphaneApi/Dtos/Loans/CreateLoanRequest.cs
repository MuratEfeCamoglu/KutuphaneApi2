namespace KutuphaneApi.Dtos.Loans;

// Tarihler istekte yok: ödünç ve son iade tarihini iş kuralına göre sunucu belirler.
public record CreateLoanRequest(int BookId, int MemberId);
