using FluentValidation;
using KutuphaneApi.Common.Exceptions;
using KutuphaneApi.Data;
using KutuphaneApi.Dtos.Loans;
using KutuphaneApi.Dtos.Members;
using KutuphaneApi.Mappings;
using KutuphaneApi.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KutuphaneApi.Services;

public class MemberService(
    AppDbContext context,
    TimeProvider timeProvider,
    IValidator<CreateMemberRequest> createValidator,
    IValidator<UpdateMemberRequest> updateValidator) : IMemberService
{
    public async Task<IReadOnlyList<MemberDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Members
            .AsNoTracking()
            .OrderBy(m => m.LastName).ThenBy(m => m.FirstName)
            .SelectDto()
            .ToListAsync(cancellationToken);
    }

    public async Task<MemberDto> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await context.Members
            .AsNoTracking()
            .Where(m => m.Id == id)
            .SelectDto()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Üye", id);
    }

    public async Task<IReadOnlyList<LoanDto>> GetLoansAsync(int id, CancellationToken cancellationToken)
    {
        // Üye yoksa boş liste değil 404 dönmeli; "kaydı yok" ile "ödüncü yok" farklı durumlar.
        if (!await context.Members.AnyAsync(m => m.Id == id, cancellationToken))
        {
            throw new NotFoundException("Üye", id);
        }

        return await context.Loans
            .AsNoTracking()
            .Where(l => l.MemberId == id)
            .OrderByDescending(l => l.LoanDate).ThenByDescending(l => l.Id)
            .SelectDto(timeProvider.GetUtcNow().UtcDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<MemberDto> CreateAsync(CreateMemberRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureEmailIsUniqueAsync(request.Email, excludeId: null, cancellationToken);

        // Kayıt tarihi sunucunun saatinden (UTC) alınır; testlerde TimeProvider sahte bir saatle değiştirilebilir.
        var member = request.ToEntity(timeProvider.GetUtcNow().UtcDateTime);
        context.Members.Add(member);
        await context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(member.Id, cancellationToken);
    }

    public async Task UpdateAsync(int id, UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var member = await context.Members.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Üye", id);

        await EnsureEmailIsUniqueAsync(request.Email, excludeId: id, cancellationToken);

        request.ApplyTo(member);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var member = await context.Members.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException("Üye", id);

        // İş kuralı 7: iade edilmemiş ödüncü olan üye silinemez.
        if (await context.Loans.AnyAsync(l => l.MemberId == id && l.ReturnDate == null, cancellationToken))
        {
            throw new BusinessRuleException("İade edilmemiş ödüncü olan üye silinemez.");
        }

        // Kitap silmedeki gibi: iade edilmiş ödünç geçmişi üyeyle birlikte, aynı transaction'da silinir.
        var returnedLoans = await context.Loans.Where(l => l.MemberId == id).ToListAsync(cancellationToken);
        context.Loans.RemoveRange(returnedLoans);
        context.Members.Remove(member);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureEmailIsUniqueAsync(string email, int? excludeId, CancellationToken cancellationToken)
    {
        if (await context.Members.AnyAsync(m => m.Email == email && m.Id != excludeId, cancellationToken))
        {
            throw new BusinessRuleException($"'{email}' e-posta adresiyle kayıtlı bir üye zaten var.");
        }
    }
}
