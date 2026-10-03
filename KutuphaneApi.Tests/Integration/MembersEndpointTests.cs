using System.Net;
using System.Net.Http.Json;
using KutuphaneApi.Dtos.Members;
using Microsoft.AspNetCore.Mvc;

namespace KutuphaneApi.Tests.Integration;

public sealed class MembersEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task Post_ValidMember_SetsCreatedAtFromTimeProvider()
    {
        var member = await CreateMemberAsync();

        Assert.Equal(Factory.Time.GetUtcNow().UtcDateTime, member.CreatedAt);
        Assert.Equal(0, member.ActiveLoanCount);
    }

    [Fact]
    public async Task Post_InvalidEmail_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/members",
            new CreateMemberRequest("Can", "Öztürk", "gecersiz-eposta", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadAsync<ValidationProblemDetails>(response);
        Assert.Contains("Email", problem.Errors.Keys);
    }

    [Fact]
    public async Task Post_DuplicateEmail_Returns409()
    {
        await CreateMemberAsync("ayse@example.com");

        var response = await Client.PostAsJsonAsync("/api/members",
            new CreateMemberRequest("Başka", "Biri", "AYSE@example.com", null));

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetLoans_UnknownMember_Returns404()
    {
        var response = await Client.GetAsync("/api/members/999/loans");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}
