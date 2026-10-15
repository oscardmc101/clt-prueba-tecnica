using Clt.Api.Application.Currencies;
using Clt.Api.Application.Currencies.Commands;
using Clt.Api.Application.Currencies.Queries;
using Clt.Api.Application.CurrencyConversion;

namespace Clt.Api.Api.Endpoints;

public static class CurrencyEndpoints
{
    public static void MapCurrencyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/currencies").WithTags("Currencies");
        group.MapGet("", async (ListCurrenciesHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ListCurrenciesQuery(), ct))).Produces<List<CurrencyResponse>>();
        group.MapPost("", async (CreateCurrencyCommand request, CreateCurrencyHandler handler, CancellationToken ct) =>
        {
            var currency = await handler.HandleAsync(request, ct);
            return Results.Created("/currencies", currency);
        }).Produces<CurrencyResponse>(201).ProducesValidationProblem().ProducesProblem(409);
        app.MapPost("/currency/convert", async (ConvertCurrencyQuery request, ConvertCurrencyHandler handler,
            CancellationToken ct) => Results.Ok(await handler.HandleAsync(request, ct)))
            .WithTags("Currency conversion").Produces<ConversionResponse>()
            .ProducesValidationProblem().ProducesProblem(404);
    }
}
