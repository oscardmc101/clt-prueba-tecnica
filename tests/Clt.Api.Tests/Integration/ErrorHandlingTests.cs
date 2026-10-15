using System.Net;
using System.Net.Http.Json;
using System.Text;
using Clt.Api.Application.Users;
using Clt.Api.Tests.Support;

namespace Clt.Api.Tests.Integration;

public class ErrorHandlingTests
{
    [Theory]
    [InlineData("{bad-json}")]
    [InlineData("null")]
    public async Task Malformed_or_null_body_returns_problem_without_internal_details(string json)
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var response = await client.PostAsync("/users", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("stackTrace", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Updating_to_an_existing_email_returns_409_and_does_not_change_user()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        await client.PostAsJsonAsync("/users", new { name = "A", email = "a@test.com" });
        var second = await client.PostAsJsonAsync("/users", new { name = "B", email = "b@test.com" });
        var user = (await second.Content.ReadFromJsonAsync<UserResponse>())!;
        var response = await client.PutAsJsonAsync($"/users/{user.Id}", new { name = "Changed", email = "a@test.com", isActive = false });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(user, await client.GetFromJsonAsync<UserResponse>($"/users/{user.Id}"));
    }

    [Fact]
    public async Task Conversion_overflow_returns_400()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/currency/convert",
            new { fromCurrencyCode = "USD", toCurrencyCode = "PYG", amount = decimal.MaxValue })).StatusCode);
    }
}
