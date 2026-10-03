using System.Net;
using System.Net.Http.Json;
using KutuphaneApi.Dtos.Authors;
using Microsoft.AspNetCore.Mvc;

namespace KutuphaneApi.Tests.Integration;

public sealed class AuthorsEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task Post_ValidAuthor_Returns201WithLocationAndBody()
    {
        var response = await Client.PostAsJsonAsync("/api/authors", new CreateAuthorRequest("Yaşar", "Kemal", 1923));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var author = await ReadAsync<AuthorDto>(response);
        Assert.Equal("Yaşar", author.FirstName);
        Assert.Equal($"/api/authors/{author.Id}", response.Headers.Location?.AbsolutePath);

        var fetched = await Client.GetFromJsonAsync<AuthorDto>($"/api/authors/{author.Id}", JsonOptions);
        Assert.Equal(author, fetched);
    }

    [Fact]
    public async Task Post_InvalidAuthor_Returns400WithFieldErrors()
    {
        var response = await Client.PostAsJsonAsync("/api/authors", new CreateAuthorRequest("", "Kemal", 3000));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains("FirstName", problem.Errors.Keys);
        Assert.Contains("BirthYear", problem.Errors.Keys);
    }

    [Fact]
    public async Task Get_UnknownAuthor_Returns404ProblemDetails()
    {
        var response = await Client.GetAsync("/api/authors/999");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AuthorWithBooks_Returns409()
    {
        var author = await CreateAuthorAsync();
        await CreateBookAsync(author.Id);

        var response = await Client.DeleteAsync($"/api/authors/{author.Id}");

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }
}
