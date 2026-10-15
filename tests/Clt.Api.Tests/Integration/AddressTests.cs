using System.Net;
using System.Net.Http.Json;
using Clt.Api.Application.Addresses;
using Clt.Api.Application.Users;
using Clt.Api.Tests.Support;

namespace Clt.Api.Tests.Integration;

public class AddressTests
{
    [Fact]
    public async Task Address_crud_and_user_cascade_work()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var userResponse = await client.PostAsJsonAsync("/users", new { name = "Juan", email = "juan@test.com" });
        var user = (await userResponse.Content.ReadFromJsonAsync<UserResponse>())!;
        var route = $"/users/{user.Id}/addresses";
        Assert.Empty((await client.GetFromJsonAsync<AddressResponse[]>(route))!);
        var created = await client.PostAsJsonAsync(route,
            new { street = "Calle Falsa 123", city = "Asunción", country = "Paraguay", zipCode = "9999" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var address = (await created.Content.ReadFromJsonAsync<AddressResponse>())!;
        Assert.Equal(user.Id, address.UserId);
        Assert.Single((await client.GetFromJsonAsync<AddressResponse[]>(route))!);
        var updated = await client.PutAsJsonAsync($"/addresses/{address.Id}",
            new { street = "Nueva 456", city = "Luque", country = "Paraguay" });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal("Luque", (await updated.Content.ReadFromJsonAsync<AddressResponse>())!.City);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/addresses/{address.Id}")).StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<AddressResponse[]>(route))!);
        var second = await client.PostAsJsonAsync(route, new { street = "A", city = "B", country = "C" });
        var secondAddress = (await second.Content.ReadFromJsonAsync<AddressResponse>())!;
        await client.DeleteAsync($"/users/{user.Id}");
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/addresses/{secondAddress.Id}")).StatusCode);
    }

    [Fact]
    public async Task Missing_parent_and_missing_address_return_404()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var body = new { street = "A", city = "B", country = "C" };
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/users/999/addresses", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/users/999/addresses")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/addresses/999", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/addresses/999")).StatusCode);
    }

    [Fact]
    public async Task Address_fields_are_validated_on_create_and_update()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var body = new { street = "", city = "", country = "" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/users/999/addresses", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/addresses/999", body)).StatusCode);
    }
}
