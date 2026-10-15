using Clt.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Users.Queries;

public sealed record ListUsersQuery(bool? IsActive);

public sealed class ListUsersHandler(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<UserResponse>> HandleAsync(ListUsersQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var users = db.Users.AsNoTracking();
        if (query.IsActive.HasValue) users = users.Where(user => user.IsActive == query.IsActive.Value);
        return await users.OrderBy(user => user.Id)
            .Select(user => new UserResponse(user.Id, user.Name, user.Email, user.IsActive)).ToListAsync(ct);
    }
}
