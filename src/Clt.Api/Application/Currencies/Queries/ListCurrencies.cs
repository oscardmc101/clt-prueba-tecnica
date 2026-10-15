using Clt.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Currencies.Queries;

public sealed record ListCurrenciesQuery;

public sealed class ListCurrenciesHandler(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<CurrencyResponse>> HandleAsync(ListCurrenciesQuery query, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Currencies.AsNoTracking().OrderBy(currency => currency.Code)
            .Select(currency => new CurrencyResponse(currency.Id, currency.Code, currency.Name, currency.RateToBase))
            .ToListAsync(ct);
    }
}
