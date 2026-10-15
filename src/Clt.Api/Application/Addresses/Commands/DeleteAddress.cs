using Clt.Api.Api.Errors;
using Clt.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Addresses.Commands;

public sealed record DeleteAddressCommand(int Id);

public sealed class DeleteAddressHandler(IDbContextFactory<AppDbContext> factory)
{
    public async Task HandleAsync(DeleteAddressCommand command, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (await db.Addresses.Where(address => address.Id == command.Id).ExecuteDeleteAsync(ct) == 0)
            throw ApiException.NotFound("Address");
    }
}
