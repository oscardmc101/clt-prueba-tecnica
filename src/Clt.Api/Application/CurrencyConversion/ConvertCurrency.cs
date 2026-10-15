using Clt.Api.Api.Errors;
using Clt.Api.Application.Common;
using Clt.Api.Domain.Entities;
using Clt.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.CurrencyConversion;

public sealed record ConvertCurrencyQuery(string FromCurrencyCode, string ToCurrencyCode, decimal Amount);
public sealed record ConversionResponse(string FromCurrency, string ToCurrency, decimal OriginalAmount,
    decimal ConvertedAmount);

public sealed class ConvertCurrencyValidator : AbstractValidator<ConvertCurrencyQuery>
{
    public ConvertCurrencyValidator()
    {
        RuleFor(request => request.FromCurrencyCode).NotEmpty();
        RuleFor(request => request.ToCurrencyCode).NotEmpty();
        RuleFor(request => request.Amount).GreaterThan(0);
    }
}

public sealed class ConvertCurrencyHandler(IDbContextFactory<AppDbContext> factory,
    IValidator<ConvertCurrencyQuery> validator)
{
    public async Task<ConversionResponse> HandleAsync(ConvertCurrencyQuery query, CancellationToken ct)
    {
        await validator.EnsureValidAsync(query, ct);
        var fromCode = query.FromCurrencyCode.Trim().ToUpperInvariant();
        var toCode = query.ToCurrencyCode.Trim().ToUpperInvariant();
        if (fromCode == toCode)
        {
            await FindCurrencyAsync(fromCode, ct);
            return new ConversionResponse(fromCode, toCode, query.Amount, query.Amount);
        }

        // Microsoft.Data.Sqlite performs synchronous I/O even through async methods.
        // Schedule both reads explicitly so Task.WhenAll joins genuinely concurrent work.
        var fromTask = Task.Run(() => FindCurrencyAsync(fromCode, ct), ct);
        var toTask = Task.Run(() => FindCurrencyAsync(toCode, ct), ct);
        await Task.WhenAll(fromTask, toTask);
        var from = await fromTask;
        var to = await toTask;
        try
        {
            var baseAmount = checked(query.Amount * from.RateToBase);
            var convertedAmount = baseAmount / to.RateToBase;
            return new ConversionResponse(from.Code, to.Code, query.Amount, convertedAmount);
        }
        catch (OverflowException)
        {
            throw new ApiException(400, "The amount exceeds the supported decimal range for this conversion.");
        }
    }

    private async Task<Currency> FindCurrencyAsync(string code, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Currencies.AsNoTracking().SingleOrDefaultAsync(currency => currency.Code == code, ct)
            ?? throw ApiException.NotFound($"Currency '{code}'");
    }
}
