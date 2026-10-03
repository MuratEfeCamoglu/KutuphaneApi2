using KutuphaneApi.Dtos.Loans;
using KutuphaneApi.Dtos.Members;

namespace KutuphaneApi.Services.Interfaces;

public interface IMemberService
{
    Task<IReadOnlyList<MemberDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<MemberDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<LoanDto>> GetLoansAsync(int id, CancellationToken cancellationToken);
    Task<MemberDto> CreateAsync(CreateMemberRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(int id, UpdateMemberRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
