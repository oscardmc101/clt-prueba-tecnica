namespace Clt.Api.Application.Currencies;

public sealed record CurrencyResponse(int Id, string Code, string Name, decimal RateToBase);
