using KutuphaneApi.Dtos.Loans;

namespace KutuphaneApi.Services.Interfaces;

public interface ILoanService
{
    Task<LoanDto> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<LoanDto> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken);
    Task ReturnAsync(int id, CancellationToken cancellationToken);
}
