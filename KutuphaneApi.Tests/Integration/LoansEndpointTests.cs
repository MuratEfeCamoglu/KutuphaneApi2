using System.Net;
using System.Net.Http.Json;
using KutuphaneApi.Common.Pagination;
using KutuphaneApi.Dtos.Loans;
using KutuphaneApi.Services;

namespace KutuphaneApi.Tests.Integration;

public sealed class LoansEndpointTests : IntegrationTestBase
{
    [Fact]
    public async Task BorrowAndReturn_FullFlow_UpdatesStatusAndAvailability()
    {
        var author = await CreateAuthorAsync();
        var book = await CreateBookAsync(author.Id, stockCount: 1);
        var member = await CreateMemberAsync();

        var borrowResponse = await Client.PostAsJsonAsync("/api/loans", new CreateLoanRequest(book.Id, member.Id));
        Assert.Equal(HttpStatusCode.Created, borrowResponse.StatusCode);
        var loan = await ReadAsync<LoanDto>(borrowResponse);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Equal(loan.LoanDate.AddDays(LoanService.LoanPeriodDays), loan.DueDate);

        var returnResponse = await Client.PostAsync($"/api/loans/{loan.Id}/return", content: null);
        Assert.Equal(HttpStatusCode.NoContent, returnResponse.StatusCode);

        var memberLoans = await Client.GetFromJsonAsync<List<LoanDto>>($"/api/members/{member.Id}/loans", JsonOptions);
        Assert.Equal(LoanStatus.Returned, Assert.Single(memberLoans!).Status);
    }

    [Fact]
    public async Task Post_MemberWithOverdueLoan_Returns409()
    {
        var author = await CreateAuthorAsync();
        var firstBook = await CreateBookAsync(author.Id, isbn: "9780000000001");
        var secondBook = await CreateBookAsync(author.Id, isbn: "9780000000002");
        var member = await CreateMemberAsync();
        await Client.PostAsJsonAsync("/api/loans", new CreateLoanRequest(firstBook.Id, member.Id));

        // API'nin kullandığı sahte saati ileri sar: ilk ödünç gecikmiş olur.
        Factory.Time.Advance(TimeSpan.FromDays(LoanService.LoanPeriodDays + 1));

        var overdue = await Client.GetFromJsonAsync<PagedResult<LoanDto>>("/api/loans?status=Overdue", JsonOptions);
        Assert.Single(overdue!.Items);

        var response = await Client.PostAsJsonAsync("/api/loans", new CreateLoanRequest(secondBook.Id, member.Id));
        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Return_AlreadyReturnedLoan_Returns409()
    {
        var author = await CreateAuthorAsync();
        var book = await CreateBookAsync(author.Id);
        var member = await CreateMemberAsync();
        var loan = await ReadAsync<LoanDto>(await Client.PostAsJsonAsync("/api/loans", new CreateLoanRequest(book.Id, member.Id)));
        await Client.PostAsync($"/api/loans/{loan.Id}/return", content: null);

        var response = await Client.PostAsync($"/api/loans/{loan.Id}/return", content: null);

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_UnknownBook_Returns404()
    {
        var member = await CreateMemberAsync();

        var response = await Client.PostAsJsonAsync("/api/loans", new CreateLoanRequest(999, member.Id));

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}
