namespace KutuphaneApi.Dtos.Loans;

// Ödüncün durumu veritabanında saklanmaz; ReturnDate, DueDate ve "şu an"dan her sorguda hesaplanır.
// JSON'da sayı (0, 1, 2) yerine metin ("Active") olarak yazılır (Program.cs → JsonStringEnumConverter).
public enum LoanStatus
{
    Active,
    Overdue,
    Returned
}
