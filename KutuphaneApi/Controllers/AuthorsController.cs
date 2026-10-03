using KutuphaneApi.Dtos.Authors;
using KutuphaneApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KutuphaneApi.Controllers;

// [ApiController]: JSON gövdesini otomatik okur, hatalı istekte otomatik 400 döner.
// [Route("api/[controller]")]: "[controller]" sınıf adındaki "Controller" ekini atar → /api/authors.
[ApiController]
[Route("api/[controller]")]
public class AuthorsController(IAuthorService authorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuthorDto>>> GetAll(CancellationToken cancellationToken)
    {
        // CancellationToken: istemci bağlantıyı keserse ASP.NET Core bunu iptal eder, veritabanı sorgusu da durur.
        return Ok(await authorService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthorDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await authorService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthorDto>> Create(CreateAuthorRequest request, CancellationToken cancellationToken)
    {
        var author = await authorService.CreateAsync(request, cancellationToken);

        // 201 Created + Location başlığı (/api/authors/{id}) + oluşan kaydın kendisi.
        return CreatedAtAction(nameof(GetById), new { id = author.Id }, author);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateAuthorRequest request, CancellationToken cancellationToken)
    {
        await authorService.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await authorService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
