using System.Net;
using System.Net.Http.Json;
using KutuphaneApi.Common.Pagination;
using KutuphaneApi.Dtos.Books;
using Microsoft.AspNetCore.Mvc;

namespace KutuphaneApi.Tests.Integration;

public sealed class BooksEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task GetById_ReturnsAuthorCategoriesAndAvailableCopies()
    {
        var author = await CreateAuthorAsync();
        var category = await CreateCategoryAsync("Roman");
        var created = await CreateBookAsync(author.Id, stockCount: 3, categoryIds: category.Id);

        var book = await Client.GetFromJsonAsync<BookDetailDto>($"/api/books/{created.Id}", JsonOptions);

        Assert.NotNull(book);
        Assert.Equal("Sabahattin Ali", book.Author.FullName);
        Assert.Equal("Roman", Assert.Single(book.Categories).Name);
        Assert.Equal(3, book.AvailableCopies);
    }

    [Fact]
    public async Task GetAll_WithSearchAndPaging_ReturnsPagedResult()
    {
        var author = await CreateAuthorAsync();
        for (var i = 1; i <= 3; i++)
        {
            var request = new CreateBookRequest($"Seri Kitap {i}", $"978000000000{i}", 2000 + i, 1, author.Id, []);
            Assert.Equal(HttpStatusCode.Created, (await Client.PostAsJsonAsync("/api/books", request)).StatusCode);
        }
        await Client.PostAsJsonAsync("/api/books", new CreateBookRequest("Başka", "9780000000009", 1990, 1, author.Id, []));

        var page = await Client.GetFromJsonAsync<PagedResult<BookListItemDto>>(
            "/api/books?search=seri&sortBy=year&sortDirection=desc&pageSize=2&page=1", JsonOptions);

        Assert.NotNull(page);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(["Seri Kitap 3", "Seri Kitap 2"], page.Items.Select(b => b.Title));
    }

    [Fact]
    public async Task Post_DuplicateIsbn_Returns409()
    {
        var author = await CreateAuthorAsync();
        await CreateBookAsync(author.Id, isbn: "9789753638029");

        var response = await Client.PostAsJsonAsync("/api/books",
            new CreateBookRequest("Kopya", "9789753638029", 1943, 1, author.Id, []));

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetAll_PageSizeAboveLimit_Returns400()
    {
        var response = await Client.GetAsync("/api/books?pageSize=51");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains("PageSize", problem.Errors.Keys);
    }
}
