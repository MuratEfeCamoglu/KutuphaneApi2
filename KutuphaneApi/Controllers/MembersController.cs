using KutuphaneApi.Dtos.Members;
using KutuphaneApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KutuphaneApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController(IMemberService memberService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await memberService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemberDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await memberService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MemberDto>> Create(CreateMemberRequest request, CancellationToken cancellationToken)
    {
        var member = await memberService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = member.Id }, member);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        await memberService.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await memberService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
