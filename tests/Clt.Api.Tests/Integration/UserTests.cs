using System.Net;
using System.Net.Http.Json;
using Clt.Api.Application.Users;
using Clt.Api.Domain.Entities;
using Clt.Api.Infrastructure.Persistence;
using Clt.Api.Tests.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clt.Api.Tests.Integration;

public class UserTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Put_without_isActive_preserves_current_state(bool initiallyActive)
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var created = await client.PostAsJsonAsync("/users", new { name = "Juan", email = "juan@test.com" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.True(user.IsActive);
        if (!initiallyActive)
        {
            var deactivated = await client.PutAsJsonAsync($"/users/{user.Id}",
                new { name = user.Name, email = user.Email, isActive = false });
            Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
            Assert.False((await deactivated.Content.ReadFromJsonAsync<UserResponse>())!.IsActive);
        }

        var response = await client.PutAsJsonAsync($"/users/{user.Id}",
            new { name = "Juan Updated", email = "updated@test.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.Equal(initiallyActive, updated.IsActive);
        Assert.Equal("Juan Updated", updated.Name);
        Assert.Equal("updated@test.com", updated.Email);
        Assert.Equal(updated, await client.GetFromJsonAsync<UserResponse>($"/users/{user.Id}"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Put_with_explicit_isActive_changes_state(bool isActive)
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var created = await client.PostAsJsonAsync("/users", new { name = "Juan", email = "juan@test.com" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = (await created.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.True(user.IsActive);
        if (isActive)
        {
            var deactivated = await client.PutAsJsonAsync($"/users/{user.Id}",
                new { name = user.Name, email = user.Email, isActive = false });
            Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
            Assert.False((await deactivated.Content.ReadFromJsonAsync<UserResponse>())!.IsActive);
        }

        var response = await client.PutAsJsonAsync($"/users/{user.Id}",
            new { name = "Juan Updated", email = "updated@test.com", isActive });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.Equal(isActive, updated.IsActive);
        Assert.Equal(updated, await client.GetFromJsonAsync<UserResponse>($"/users/{user.Id}"));
    }

    [Fact]
    public async Task User_crud_filter_and_default_active_work_without_password()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/users", new { name = "Juan", email = "juan@test.com" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = (await response.Content.ReadFromJsonAsync<UserResponse>())!;
        Assert.True(user.IsActive);
        Assert.Equal($"/users/{user.Id}", response.Headers.Location?.ToString());
        Assert.Equal(user, await client.GetFromJsonAsync<UserResponse>($"/users/{user.Id}"));
        var updated = await client.PutAsJsonAsync($"/users/{user.Id}",
            new { name = "Juan Updated", email = "updated@test.com", isActive = false });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<UserResponse[]>("/users?isActive=true"))!);
        Assert.Single((await client.GetFromJsonAsync<UserResponse[]>("/users?isActive=false"))!);
        Assert.Single((await client.GetFromJsonAsync<UserResponse[]>("/users"))!);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/users/{user.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/users/{user.Id}")).StatusCode);
    }

    [Theory]
    [InlineData("", "valid@test.com")]
    [InlineData("Name", "invalid")]
    public async Task Invalid_user_returns_validation_problem(string name, string email)
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/users", new { name, email });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("errors", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Duplicate_email_is_conflict_even_with_different_case()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/users", new { name = "A", email = "a@test.com" })).StatusCode);
        var response = await client.PostAsJsonAsync("/users", new { name = "B", email = "A@TEST.COM" });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Password_is_hashed_and_never_returned()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        const string password = "Test-password-123!";
        var response = await client.PostAsJsonAsync("/users", new { name = "A", email = "a@test.com", password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        var factory = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        var stored = await db.Users.SingleAsync();
        Assert.NotNull(stored.PasswordHash);
        Assert.NotEqual(password, stored.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(stored, stored.PasswordHash, password));
        Assert.DoesNotContain("password", await client.GetStringAsync("/users"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_user_returns_404_on_read_update_delete()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/users/999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/users/999",
            new { name = "A", email = "a@test.com", isActive = true })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/users/999")).StatusCode);
    }
}
