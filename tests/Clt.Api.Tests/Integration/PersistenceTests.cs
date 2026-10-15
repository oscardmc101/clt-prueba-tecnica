using Clt.Api.Domain.Entities;
using Clt.Api.Infrastructure.Persistence;
using Clt.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Clt.Api.Tests.Integration;

public class PersistenceTests
{
    [Fact]
    public async Task Migration_seed_unique_indexes_and_cascade_work_on_real_sqlite()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var factory = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal(1m, (await db.Currencies.SingleAsync(c => c.Code == "PYG")).RateToBase);
        Assert.Equal(7300m, (await db.Currencies.SingleAsync(c => c.Code == "USD")).RateToBase);
        var user = new User { Name = "Parent", Email = "parent@test.com" };
        user.Addresses.Add(new Address { Street = "A", City = "B", Country = "C" });
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await using (var duplicate = await factory.CreateDbContextAsync())
        {
            duplicate.Users.Add(new User { Name = "Duplicate", Email = "PARENT@test.com" });
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
        }
        await using (var duplicate = await factory.CreateDbContextAsync())
        {
            duplicate.Currencies.Add(new Currency { Code = "usd", Name = "Duplicate", RateToBase = 2m });
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
        }
        db.ChangeTracker.Clear();
        db.Users.Remove(await db.Users.SingleAsync());
        await db.SaveChangesAsync();
        Assert.Empty(await db.Addresses.AsNoTracking().ToListAsync());
    }
}
