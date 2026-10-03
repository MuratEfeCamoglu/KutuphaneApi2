using FluentValidation;
using KutuphaneApi.Common.Exceptions;
using KutuphaneApi.Data;
using KutuphaneApi.Dtos.Loans;
using KutuphaneApi.Services;
using KutuphaneApi.Validators;
using Microsoft.Extensions.Time.Testing;

namespace KutuphaneApi.Tests.Unit;

// xUnit her test metodu için sınıftan yeni bir örnek oluşturur: her test kendi temiz veritabanıyla başlar.
public sealed class LoanServiceTests : IDisposable
{
    private static readonly DateTimeOffset Start = new(2026, 3, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly TestDatabase _db = new();

    // FakeTimeProvider: saati elle ayarlayıp ileri sarabildiğimiz sahte saat. Gecikme kuralı bununla test edilir.
    private readonly FakeTimeProvider _time = new(Start);

    private LoanService CreateService(AppDbContext context) =>
        new(context, _time, new CreateLoanRequestValidator(), new LoanQueryParametersValidator());

    private async Task<LoanDto> BorrowAsync(int bookId, int memberId)
    {
        await using var context = _db.CreateContext();
        return await CreateService(context).CreateAsync(new CreateLoanRequest(bookId, memberId), CancellationToken.None);
    }

    public void Dispose() => _db.Dispose();

    // Kural 1
    [Fact]
    public async Task CreateAsync_ValidRequest_SetsDueDateFourteenDaysAfterLoanDate()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id);
        var member = _db.AddMember();

        var loan = await BorrowAsync(book.Id, member.Id);

        Assert.Equal(Start.UtcDateTime, loan.LoanDate);
        Assert.Equal(Start.UtcDateTime.AddDays(LoanService.LoanPeriodDays), loan.DueDate);
        Assert.Null(loan.ReturnDate);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    // Kural 2
    [Fact]
    public async Task CreateAsync_MemberAlreadyHasMaxActiveLoans_ThrowsBusinessRuleException()
    {
        var author = _db.AddAuthor();
        var member = _db.AddMember();
        for (var i = 1; i <= LoanService.MaxActiveLoansPerMember; i++)
        {
            var book = _db.AddBook(author.Id, isbn: $"978000000000{i}");
            await BorrowAsync(book.Id, member.Id);
        }
        var extraBook = _db.AddBook(author.Id, isbn: "9780000000009");

        await Assert.ThrowsAsync<BusinessRuleException>(() => BorrowAsync(extraBook.Id, member.Id));
    }

    // Kural 3
    [Fact]
    public async Task CreateAsync_NoAvailableCopy_ThrowsBusinessRuleException()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id, stockCount: 1);
        var firstMember = _db.AddMember("bir@example.com");
        var secondMember = _db.AddMember("iki@example.com");
        await BorrowAsync(book.Id, firstMember.Id);

        await Assert.ThrowsAsync<BusinessRuleException>(() => BorrowAsync(book.Id, secondMember.Id));
    }

    // Kural 3: iade edilen kopya tekrar müsait olur.
    [Fact]
    public async Task CreateAsync_CopyReturned_BookCanBeBorrowedAgain()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id, stockCount: 1);
        var firstMember = _db.AddMember("bir@example.com");
        var secondMember = _db.AddMember("iki@example.com");
        var firstLoan = await BorrowAsync(book.Id, firstMember.Id);

        await using (var context = _db.CreateContext())
        {
            await CreateService(context).ReturnAsync(firstLoan.Id, CancellationToken.None);
        }

        var secondLoan = await BorrowAsync(book.Id, secondMember.Id);
        Assert.Equal(secondMember.Id, secondLoan.MemberId);
    }

    // Kural 4
    [Fact]
    public async Task CreateAsync_MemberHasSameBookNotReturned_ThrowsBusinessRuleException()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id, stockCount: 5);
        var member = _db.AddMember();
        await BorrowAsync(book.Id, member.Id);

        await Assert.ThrowsAsync<BusinessRuleException>(() => BorrowAsync(book.Id, member.Id));
    }

    // Kural 5
    [Fact]
    public async Task CreateAsync_MemberHasOverdueLoan_ThrowsBusinessRuleException()
    {
        var author = _db.AddAuthor();
        var firstBook = _db.AddBook(author.Id, isbn: "9780000000001");
        var secondBook = _db.AddBook(author.Id, isbn: "9780000000002");
        var member = _db.AddMember();
        await BorrowAsync(firstBook.Id, member.Id);

        // Saati son iade tarihinin bir gün sonrasına sar: ilk ödünç artık gecikmiş.
        _time.Advance(TimeSpan.FromDays(LoanService.LoanPeriodDays + 1));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => BorrowAsync(secondBook.Id, member.Id));
        Assert.Contains("Gecikmiş", exception.Message);
    }

    // Kural 5'in sınırı: son iade gününe kadar ödünç gecikmiş sayılmaz.
    [Fact]
    public async Task CreateAsync_OnDueDate_LoanIsNotOverdue()
    {
        var author = _db.AddAuthor();
        var firstBook = _db.AddBook(author.Id, isbn: "9780000000001");
        var secondBook = _db.AddBook(author.Id, isbn: "9780000000002");
        var member = _db.AddMember();
        await BorrowAsync(firstBook.Id, member.Id);

        _time.Advance(TimeSpan.FromDays(LoanService.LoanPeriodDays));

        var loan = await BorrowAsync(secondBook.Id, member.Id);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    // Kural 6
    [Fact]
    public async Task ReturnAsync_AlreadyReturned_ThrowsBusinessRuleException()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id);
        var member = _db.AddMember();
        var loan = await BorrowAsync(book.Id, member.Id);

        await using var context = _db.CreateContext();
        var service = CreateService(context);
        await service.ReturnAsync(loan.Id, CancellationToken.None);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.ReturnAsync(loan.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ReturnAsync_ActiveLoan_SetsReturnDateToNow()
    {
        var author = _db.AddAuthor();
        var book = _db.AddBook(author.Id);
        var member = _db.AddMember();
        var loan = await BorrowAsync(book.Id, member.Id);
        _time.Advance(TimeSpan.FromDays(3));

        await using var context = _db.CreateContext();
        var service = CreateService(context);
        await service.ReturnAsync(loan.Id, CancellationToken.None);

        var returned = await service.GetByIdAsync(loan.Id, CancellationToken.None);
        Assert.Equal(Start.UtcDateTime.AddDays(3), returned.ReturnDate);
        Assert.Equal(LoanStatus.Returned, returned.Status);
    }

    [Fact]
    public async Task GetPagedAsync_StatusFilter_ReturnsLoansWithMatchingComputedStatus()
    {
        var author = _db.AddAuthor();
        var returnedBook = _db.AddBook(author.Id, isbn: "9780000000001");
        var overdueBook = _db.AddBook(author.Id, isbn: "9780000000002");
        var activeBook = _db.AddBook(author.Id, isbn: "9780000000003");
        var member = _db.AddMember();

        var returnedLoan = await BorrowAsync(returnedBook.Id, member.Id);
        var overdueLoan = await BorrowAsync(overdueBook.Id, member.Id);
        await using (var context = _db.CreateContext())
        {
            await CreateService(context).ReturnAsync(returnedLoan.Id, CancellationToken.None);
        }

        // 20 gün sonra: ikinci ödünç gecikmiş. Bu tarihte yeni ödünç alınamayacağı için
        // üçüncü ödüncü veritabanına doğrudan ekliyoruz.
        _time.Advance(TimeSpan.FromDays(20));
        await using (var context = _db.CreateContext())
        {
            var now = _time.GetUtcNow().UtcDateTime;
            context.Loans.Add(new() { BookId = activeBook.Id, MemberId = member.Id, LoanDate = now, DueDate = now.AddDays(14) });
            await context.SaveChangesAsync();
        }

        await using var queryContext = _db.CreateContext();
        var service = CreateService(queryContext);

        var overdue = await service.GetPagedAsync(new LoanQueryParameters { Status = LoanStatus.Overdue }, CancellationToken.None);
        var returned = await service.GetPagedAsync(new LoanQueryParameters { Status = LoanStatus.Returned }, CancellationToken.None);
        var active = await service.GetPagedAsync(new LoanQueryParameters { Status = LoanStatus.Active }, CancellationToken.None);

        Assert.Equal(overdueLoan.Id, Assert.Single(overdue.Items).Id);
        Assert.Equal(returnedLoan.Id, Assert.Single(returned.Items).Id);
        Assert.Equal(activeBook.Id, Assert.Single(active.Items).BookId);
        Assert.Equal(LoanStatus.Overdue, overdue.Items[0].Status);
    }

    [Fact]
    public async Task CreateAsync_UnknownBook_ThrowsNotFoundException()
    {
        var member = _db.AddMember();

        await Assert.ThrowsAsync<NotFoundException>(() => BorrowAsync(999, member.Id));
    }

    [Fact]
    public async Task CreateAsync_InvalidIds_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => BorrowAsync(0, -1));
    }
}
