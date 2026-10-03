using FluentValidation;
using KutuphaneApi.Common.Exceptions;
using KutuphaneApi.Common.Pagination;
using KutuphaneApi.Data;
using KutuphaneApi.Dtos.Loans;
using KutuphaneApi.Entities;
using KutuphaneApi.Mappings;
using KutuphaneApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Services;

public class LoanService(
    AppDbContext context,
    TimeProvider timeProvider,
    IValidator<CreateLoanRequest> createValidator,
    IValidator<LoanQueryParameters> queryValidator) : ILoanService
{
    // İş kurallarındaki sabitler: adı olan sabitler, koda gömülü "sihirli sayılardan" daha okunur.
    public const int LoanPeriodDays = 14;
    public const int MaxActiveLoansPerMember = 3;

    public async Task<PagedResult<LoanDto>> GetPagedAsync(LoanQueryParameters parameters, CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(parameters, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = context.Loans.AsNoTracking();

        if (parameters.Status is not null)
        {
            query = query.WhereStatus(parameters.Status.Value, now);
        }

        if (parameters.MemberId is not null)
        {
            query = query.Where(l => l.MemberId == parameters.MemberId);
        }

        if (parameters.BookId is not null)
        {
            query = query.Where(l => l.BookId == parameters.BookId);
        }

        // En yeni ödünç en üstte.
        return await query
            .OrderByDescending(l => l.LoanDate).ThenByDescending(l => l.Id)
            .SelectDto(now)
            .ToPagedResultAsync(parameters, cancellationToken);
    }

    public async Task<LoanDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Loans
            .AsNoTracking()
            .Where(l => l.Id == id)
            .SelectDto(timeProvider.GetUtcNow().UtcDateTime)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Ödünç", id);
    }

    public async Task<LoanDto> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!await context.Members.AnyAsync(m => m.Id == request.MemberId, cancellationToken))
        {
            throw new NotFoundException("Üye", request.MemberId);
        }

        // Kitabın stoğu ve şu an ödünçte olan kopya sayısı tek sorguda.
        var book = await context.Books
            .Where(b => b.Id == request.BookId)
            .Select(b => new { b.StockCount, ActiveLoanCount = b.Loans.Count(l => l.ReturnDate == null) })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Kitap", request.BookId);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Üyenin iade edilmemiş ödünçleri tek sorguda alınır; kural 2, 4 ve 5 bu liste üzerinden kontrol edilir.
        var memberActiveLoans = await context.Loans
            .Where(l => l.MemberId == request.MemberId && l.ReturnDate == null)
            .Select(l => new { l.BookId, l.DueDate })
            .ToListAsync(cancellationToken);

        // Kural 5: gecikmiş ödüncü olan üye yeni ödünç alamaz.
        if (memberActiveLoans.Any(l => l.DueDate < now))
        {
            throw new BusinessRuleException("Gecikmiş ödüncü olan üye yeni kitap ödünç alamaz. Önce gecikmiş kitabı iade etmelidir.");
        }

        // Kural 2: bir üyenin aynı anda en fazla 3 aktif ödüncü olabilir.
        if (memberActiveLoans.Count >= MaxActiveLoansPerMember)
        {
            throw new BusinessRuleException($"Bir üye aynı anda en fazla {MaxActiveLoansPerMember} kitap ödünç alabilir.");
        }

        // Kural 4: üye aynı kitabı iade etmeden ikinci kez alamaz.
        if (memberActiveLoans.Any(l => l.BookId == request.BookId))
        {
            throw new BusinessRuleException("Üye bu kitabı zaten ödünç almış ve henüz iade etmemiş.");
        }

        // Kural 3: müsait kopyası olmayan kitap ödünç verilemez.
        if (book.StockCount - book.ActiveLoanCount <= 0)
        {
            throw new BusinessRuleException("Bu kitabın müsait kopyası yok.");
        }

        // Kural 1: ödünç süresi 14 gündür.
        var loan = new Loan
        {
            BookId = request.BookId,
            MemberId = request.MemberId,
            LoanDate = now,
            DueDate = now.AddDays(LoanPeriodDays)
        };

        context.Loans.Add(loan);
        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(loan.Id, cancellationToken);
    }

    public async Task ReturnAsync(int id, CancellationToken cancellationToken)
    {
        var loan = await context.Loans.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Ödünç", id);

        // Kural 6: zaten iade edilmiş ödünç tekrar iade edilemez.
        if (loan.ReturnDate is not null)
        {
            throw new BusinessRuleException("Bu ödünç zaten iade edilmiş.");
        }

        loan.ReturnDate = timeProvider.GetUtcNow().UtcDateTime;
        await context.SaveChangesAsync(cancellationToken);
    }
}
