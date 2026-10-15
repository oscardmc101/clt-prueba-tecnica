using Clt.Api.Api.Errors;
using Clt.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Addresses.Queries;

public sealed record ListAddressesQuery(int UserId);

public sealed class ListAddressesHandler(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<AddressResponse>> HandleAsync(ListAddressesQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.Users.AnyAsync(user => user.Id == query.UserId, ct)) throw ApiException.NotFound("User");
        return await db.Addresses.AsNoTracking().Where(address => address.UserId == query.UserId)
            .OrderBy(address => address.Id).Select(address => new AddressResponse(address.Id, address.UserId,
                address.Street, address.City, address.Country, address.ZipCode)).ToListAsync(ct);
    }
}
