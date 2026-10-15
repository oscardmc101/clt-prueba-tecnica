using System.Net;
using System.Net.Http.Json;
using System.Text;
using Clt.Api.Application.Users;
using Clt.Api.Application.Users.Commands;
using Clt.Api.Tests.Support;

namespace Clt.Api.Tests.Integration;

public class BulkTests
{
    [Fact]
    public async Task Sample_json_can_be_posted_directly_and_reports_duplicates_on_repeat()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var json = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "sample-data", "bulk_users_test.json"));
        var response = await client.PostAsync("/users/bulk", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bulk = (await response.Content.ReadFromJsonAsync<BulkUsersResponse>())!;
        Assert.Equal(4, bulk.Total);
        Assert.Equal(4, bulk.CreatedCount);
        Assert.Equal(0, bulk.FailedCount);
        Assert.All(bulk.Results, item => { Assert.True(item.Success); Assert.NotNull(item.User); Assert.Null(item.Error); });
        response = await client.PostAsync("/users/bulk", new StringContent(json, Encoding.UTF8, "application/json"));
        bulk = (await response.Content.ReadFromJsonAsync<BulkUsersResponse>())!;
        Assert.Equal(0, bulk.CreatedCount);
        Assert.Equal(4, bulk.FailedCount);
        Assert.All(bulk.Results, item => Assert.Equal(409, item.Error?.Status));
    }

    [Fact]
    public async Task Mixed_bulk_preserves_successes_and_reports_each_error_in_input_order()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var users = new CreateUserCommand?[]
        {
            new("A", "same@test.com", "Optional-password!"),
            new("B", "SAME@test.com"),
            new("", "invalid"),
            null,
            new("C", "other@test.com")
        };
        var response = await client.PostAsJsonAsync("/users/bulk", new { users });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bulk = (await response.Content.ReadFromJsonAsync<BulkUsersResponse>())!;
        Assert.Equal(5, bulk.Total);
        Assert.Equal(2, bulk.CreatedCount);
        Assert.Equal(3, bulk.FailedCount);
        Assert.Equal(Enumerable.Range(0, 5), bulk.Results.Select(item => item.Index));
        Assert.Single(bulk.Results, item => item.Error?.Status == 409);
        Assert.Equal(400, bulk.Results[2].Error?.Status);
        Assert.NotNull(bulk.Results[2].Error?.Errors);
        Assert.Equal(400, bulk.Results[3].Error?.Status);
        Assert.Equal(2, (await client.GetFromJsonAsync<UserResponse[]>("/users"))!.Length);
        Assert.DoesNotContain("password", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"users\":null}")]
    [InlineData("{\"users\":[]}")]
    public async Task Invalid_container_returns_400(string json)
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/users/bulk",
            new StringContent(json, Encoding.UTF8, "application/json"))).StatusCode);
    }
}
