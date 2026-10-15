using Clt.Api.Application.Common;
using Clt.Api.Domain.Entities;
using Clt.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Currencies.Commands;

public sealed record CreateCurrencyCommand(string Code, string Name, decimal RateToBase);

public sealed class CreateCurrencyValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyValidator()
    {
        RuleFor(request => request.Code).NotEmpty();
        RuleFor(request => request.Name).NotEmpty();
        RuleFor(request => request.RateToBase).GreaterThan(0);
    }
}

public sealed class CreateCurrencyHandler(IDbContextFactory<AppDbContext> factory,
    IValidator<CreateCurrencyCommand> validator)
{
    public async Task<CurrencyResponse> HandleAsync(CreateCurrencyCommand command, CancellationToken ct)
    {
        await validator.EnsureValidAsync(command, ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var currency = new Currency
        {
            Code = command.Code.Trim().ToUpperInvariant(), Name = command.Name.Trim(), RateToBase = command.RateToBase
        };
        db.Currencies.Add(currency);
        await DatabaseErrors.SaveAsync(db, "A currency with this code already exists.", ct);
        return new CurrencyResponse(currency.Id, currency.Code, currency.Name, currency.RateToBase);
    }
}
