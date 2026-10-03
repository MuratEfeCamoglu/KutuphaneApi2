using KutuphaneApi.Dtos.Loans;
using KutuphaneApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KutuphaneApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController(ILoanService loanService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LoanDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await loanService.GetByIdAsync(id, cancellationToken));
    }

    // Ödünç verme: yeni bir Loan kaydı oluşturur → 201.
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> Create(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        var loan = await loanService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = loan.Id }, loan);
    }

    // İade: CRUD'a tam uymayan bir "eylem" olduğu için alt kaynak adresiyle (POST /api/loans/{id}/return) modellenir.
    // Mevcut kaydı güncellediği için 204 döner.
    [HttpPost("{id:int}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Return(int id, CancellationToken cancellationToken)
    {
        await loanService.ReturnAsync(id, cancellationToken);
        return NoContent();
    }
}
