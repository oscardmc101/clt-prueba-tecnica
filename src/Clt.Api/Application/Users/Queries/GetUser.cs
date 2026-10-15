using Clt.Api.Api.Errors;
using Clt.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Users.Queries;

public sealed record GetUserQuery(int Id);

public sealed class GetUserHandler(IDbContextFactory<AppDbContext> factory)
{
    public async Task<UserResponse> HandleAsync(GetUserQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Users.AsNoTracking().Where(user => user.Id == query.Id)
            .Select(user => new UserResponse(user.Id, user.Name, user.Email, user.IsActive))
            .SingleOrDefaultAsync(ct) ?? throw ApiException.NotFound("User");
    }
}
