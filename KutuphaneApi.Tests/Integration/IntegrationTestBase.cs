using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KutuphaneApi.Dtos.Authors;
using KutuphaneApi.Dtos.Books;
using KutuphaneApi.Dtos.Categories;
using KutuphaneApi.Dtos.Members;
using Microsoft.AspNetCore.Mvc;

namespace KutuphaneApi.Tests.Integration;

// Her test kendi factory'si, yani kendi boş veritabanıyla başlar; testler birbirinin verisini görmez.
public abstract class IntegrationTestBase : IDisposable
{
    // API ile aynı JSON ayarları: camelCase alan adları ve metin olarak enum'lar.
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected KutuphaneApiFactory Factory { get; } = new();
    protected HttpClient Client { get; }

    protected IntegrationTestBase()
    {
        Client = Factory.CreateClientWithDatabase();
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        Assert.NotNull(value);
        return value;
    }

    // Beklenen durum kodunu ve ProblemDetails gövdesini birlikte doğrular.
    protected static async Task<ProblemDetails> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await ReadAsync<ProblemDetails>(response);
    }

    protected async Task<AuthorDto> CreateAuthorAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/authors", new CreateAuthorRequest("Sabahattin", "Ali", 1907));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<AuthorDto>(response);
    }

    protected async Task<CategoryDto> CreateCategoryAsync(string name = "Roman")
    {
        var response = await Client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<CategoryDto>(response);
    }

    protected async Task<BookDetailDto> CreateBookAsync(int authorId, string isbn = "9789753638029", int stockCount = 1, params int[] categoryIds)
    {
        var request = new CreateBookRequest("Kürk Mantolu Madonna", isbn, 1943, stockCount, authorId, categoryIds);
        var response = await Client.PostAsJsonAsync("/api/books", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<BookDetailDto>(response);
    }

    protected async Task<MemberDto> CreateMemberAsync(string email = "ayse@example.com")
    {
        var response = await Client.PostAsJsonAsync("/api/members", new CreateMemberRequest("Ayşe", "Yılmaz", email, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<MemberDto>(response);
    }
}
