using Clt.Api.Api.Errors;
using Clt.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Users.Commands;

public sealed record DeleteUserCommand(int Id);

public sealed class DeleteUserHandler(IDbContextFactory<AppDbContext> factory)
{
    public async Task HandleAsync(DeleteUserCommand command, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var affected = await db.Users.Where(user => user.Id == command.Id).ExecuteDeleteAsync(ct);
        if (affected == 0) throw ApiException.NotFound("User");
    }
}
