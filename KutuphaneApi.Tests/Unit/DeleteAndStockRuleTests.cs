using KutuphaneApi.Common.Exceptions;
using KutuphaneApi.Dtos.Books;
using KutuphaneApi.Dtos.Loans;
using KutuphaneApi.Services;
using KutuphaneApi.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace KutuphaneApi.Tests.Unit;

// Ödünçle ilgili ama LoanService dışındaki servislerde uygulanan kurallar: 7, 8 ve 9.
public sealed class DeleteAndStockRuleTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));

    public void Dispose() => _db.Dispose();

    private async Task<LoanDto> BorrowAsync(int bookId, int memberId)
    {
        await using var context = _db.CreateContext();
        var service = new LoanService(context, _time, new CreateLoanRequestValidator(), new LoanQueryParametersValidator());
        return await service.CreateAsync(new CreateLoanRequest(bookId, memberId), CancellationToken.None);
    }

    private BookService CreateBookService(Data.AppDbContext context) =>
        new(context,
            new CreateBookRequestValidator(_time),
            new UpdateBookRequestValidator(_time),
            new BookQueryParametersValidator());

    private MemberService CreateMemberService(Data.AppDbContext context) =>
        new(context, _time, new CreateMemberRequestValidator(), new UpdateMemberRequestValidator());

    // Kural 7 (kitap)
    [Fact]
    public async Task BookDelete_WithActiveLoan_ThrowsBusinessRuleException()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id);
        var member = _db.AddMember();
        await BorrowAsync(book.Id, member.Id);

        await using var context = _db.CreateContext();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => CreateBookService(context).DeleteAsync(book.Id, CancellationToken.None));
    }

    // Kural 7'nin tersi: sadece iade edilmiş geçmişi olan kitap silinebilir, geçmişi de silinir.
    [Fact]
    public async Task BookDelete_WithOnlyReturnedLoans_DeletesBookAndLoanHistory()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id);
        var member = _db.AddMember();
        var loan = await BorrowAsync(book.Id, member.Id);
        await using (var context = _db.CreateContext())
        {
            await new LoanService(context, _time, new CreateLoanRequestValidator(), new LoanQueryParametersValidator())
                .ReturnAsync(loan.Id, CancellationToken.None);
        }

        await using (var context = _db.CreateContext())
        {
            await CreateBookService(context).DeleteAsync(book.Id, CancellationToken.None);
        }

        await using var assertContext = _db.CreateContext();
        Assert.False(await assertContext.Books.AnyAsync());
        Assert.False(await assertContext.Loans.AnyAsync());
    }

    // Kural 7 (üye)
    [Fact]
    public async Task MemberDelete_WithActiveLoan_ThrowsBusinessRuleException()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id);
        var member = _db.AddMember();
        await BorrowAsync(book.Id, member.Id);

        await using var context = _db.CreateContext();

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => CreateMemberService(context).DeleteAsync(member.Id, CancellationToken.None));
    }

    // Kural 8
    [Fact]
    public async Task AuthorDelete_WithBooks_ThrowsBusinessRuleException()
    {
        var author = _db.AddAuthor();
        _db.AddBook(author.Id);

        await using var context = _db.CreateContext();
        var service = new AuthorService(context, new CreateAuthorRequestValidator(_time), new UpdateAuthorRequestValidator(_time));

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeleteAsync(author.Id, CancellationToken.None));
    }

    // Kural 9
    [Fact]
    public async Task BookUpdate_StockBelowActiveLoanCount_ThrowsBusinessRuleException()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id, stockCount: 2);
        await BorrowAsync(book.Id, _db.AddMember("bir@example.com").Id);
        await BorrowAsync(book.Id, _db.AddMember("iki@example.com").Id);

        await using var context = _db.CreateContext();
        var request = new UpdateBookRequest(book.Title, book.Isbn, book.PublishedYear, StockCount: 1, author.Id, []);

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => CreateBookService(context).UpdateAsync(book.Id, request, CancellationToken.None));
    }

    // Kural 9'un sınırı: stok aktif ödünç sayısına eşit olabilir.
    [Fact]
    public async Task BookUpdate_StockEqualToActiveLoanCount_Succeeds()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id, stockCount: 3);
        await BorrowAsync(book.Id, _db.AddMember().Id);

        await using (var context = _db.CreateContext())
        {
            var request = new UpdateBookRequest(book.Title, book.Isbn, book.PublishedYear, StockCount: 1, author.Id, []);
            await CreateBookService(context).UpdateAsync(book.Id, request, CancellationToken.None);
        }

        await using var assertContext = _db.CreateContext();
        var detail = await CreateBookService(assertContext).GetByIdAsync(book.Id, CancellationToken.None);
        Assert.Equal(1, detail.StockCount);
        Assert.Equal(0, detail.AvailableCopies);
    }
}
