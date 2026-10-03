namespace KutuphaneApi.Entities;

public class Loan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int MemberId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }

    // null ise kitap henüz iade edilmemiştir.
    public DateTime? ReturnDate { get; set; }

    public Book Book { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
