using System.Net;
using System.Net.Http.Json;
using KutuphaneApi.Dtos.Categories;

namespace KutuphaneApi.Tests.Integration;

public sealed class CategoriesEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task PutThenDelete_ExistingCategory_Returns204AndRemovesIt()
    {
        var category = await CreateCategoryAsync("Tarih");

        var putResponse = await Client.PutAsJsonAsync($"/api/categories/{category.Id}", new UpdateCategoryRequest("Tarih ve Kültür"));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var updated = await Client.GetFromJsonAsync<CategoryDto>($"/api/categories/{category.Id}", JsonOptions);
        Assert.Equal("Tarih ve Kültür", updated?.Name);

        var deleteResponse = await Client.DeleteAsync($"/api/categories/{category.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/categories/{category.Id}")).StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateNameWithDifferentCase_Returns409()
    {
        await CreateCategoryAsync("Roman");

        var response = await Client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest("roman"));

        var problem = await AssertProblemAsync(response, HttpStatusCode.Conflict);
        Assert.Contains("roman", problem.Detail);
    }
}
