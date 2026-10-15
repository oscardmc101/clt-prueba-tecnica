using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Clt.Api.Tests.Integration;

public class SecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SecurityTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Theory]
    [InlineData(null)]
    [InlineData("incorrect")]
    public async Task Missing_or_incorrect_api_key_returns_401(string? key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        if (key is not null) request.Headers.Add("X-API-KEY", key);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Swagger_is_accessible_and_documents_api_key()
    {
        var document = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("X-API-KEY", document);
        Assert.Contains("apiKey", document);
        using var response = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
