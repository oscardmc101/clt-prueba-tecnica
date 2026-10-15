using System.Net;
using System.Net.Http.Json;
using Clt.Api.Application.Users.Commands;
using Clt.Api.Tests.Support;

namespace Clt.Api.Tests.Concurrency;

public class ConcurrentOperationsTests
{
    [Fact]
    public async Task Bulk_operations_overlap_and_every_insert_uses_a_different_context()
    {
        var probe = new ConcurrencyProbe("INSERT INTO \"Users\"");
        using var app = new ApiFactory(probe);
        using var client = app.CreateAuthorizedClient();
        probe.Enabled = true;
        var users = Enumerable.Range(1, 8).Select(index => new { name = $"User {index}", email = $"user{index}@test.com" });
        var response = await client.PostAsJsonAsync("/users/bulk", new { users });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bulk = (await response.Content.ReadFromJsonAsync<BulkUsersResponse>())!;
        Assert.Equal(8, bulk.CreatedCount);
        Assert.Equal(0, bulk.FailedCount);
        Assert.True(probe.PairObserved);
        Assert.Equal(8, probe.ContextIds.Count);
    }

    [Fact]
    public async Task Conversion_reads_overlap_and_use_two_different_contexts()
    {
        var probe = new ConcurrencyProbe("FROM \"Currencies\"");
        using var app = new ApiFactory(probe);
        using var client = app.CreateAuthorizedClient();
        probe.Enabled = true;
        var response = await client.PostAsJsonAsync("/currency/convert",
            new { fromCurrencyCode = "USD", toCurrencyCode = "PYG", amount = 100 });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(probe.PairObserved);
        Assert.Equal(2, probe.ContextIds.Count);
    }
}
