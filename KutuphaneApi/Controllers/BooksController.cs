using KutuphaneApi.Common.Pagination;
using KutuphaneApi.Dtos.Books;
using KutuphaneApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace KutuphaneApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController(IBookService bookService) : ControllerBase
{
    // [FromQuery]: parametre nesnesinin özellikleri URL'deki query string'den doldurulur.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BookListItemDto>>> GetAll(
        [FromQuery] BookQueryParameters parameters, CancellationToken cancellationToken)
    {
        return Ok(await bookService.GetPagedAsync(parameters, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await bookService.GetByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookDetailDto>> Create(CreateBookRequest request, CancellationToken cancellationToken)
    {
        var book = await bookService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateBookRequest request, CancellationToken cancellationToken)
    {
        await bookService.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await bookService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
