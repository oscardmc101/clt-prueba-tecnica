using System.Net;
using System.Net.Http.Json;
using Clt.Api.Application.Currencies;
using Clt.Api.Application.CurrencyConversion;
using Clt.Api.Tests.Support;

namespace Clt.Api.Tests.Integration;

public class CurrencyTests
{
    [Fact]
    public async Task Currencies_are_seeded_and_new_codes_are_normalized_and_unique()
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var currencies = (await client.GetFromJsonAsync<CurrencyResponse[]>("/currencies"))!;
        Assert.Equal(2, currencies.Length);
        var response = await client.PostAsJsonAsync("/currencies", new { code = "eur", name = "Euro", rateToBase = 8000m });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("EUR", (await response.Content.ReadFromJsonAsync<CurrencyResponse>())!.Code);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/currencies",
            new { code = "eUr", name = "Euro", rateToBase = 8000m })).StatusCode);
    }

    [Theory]
    [InlineData("", "Name", 1)]
    [InlineData("EUR", "", 1)]
    [InlineData("EUR", "Euro", 0)]
    [InlineData("EUR", "Euro", -1)]
    public async Task Invalid_currency_returns_400(string code, string name, decimal rateToBase)
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/currencies", new { code, name, rateToBase })).StatusCode);
    }

    [Theory]
    [InlineData("USD", "PYG", 100, 730000)]
    [InlineData("PYG", "USD", 7300, 1)]
    [InlineData("USD", "usd", 100, 100)]
    public async Task Conversion_uses_rate_to_base(string from, string to, decimal amount, decimal expected)
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        var response = await client.PostAsJsonAsync("/currency/convert",
            new { fromCurrencyCode = from, toCurrencyCode = to, amount });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conversion = (await response.Content.ReadFromJsonAsync<ConversionResponse>())!;
        Assert.Equal(expected, conversion.ConvertedAmount);
        Assert.Equal(amount, conversion.OriginalAmount);
        Assert.Equal(from.ToUpperInvariant(), conversion.FromCurrency);
        Assert.Equal(to.ToUpperInvariant(), conversion.ToCurrency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Non_positive_amount_is_invalid(decimal amount)
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/currency/convert",
            new { fromCurrencyCode = "USD", toCurrencyCode = "PYG", amount })).StatusCode);
    }

    [Theory]
    [InlineData("XXX", "PYG")]
    [InlineData("USD", "XXX")]
    [InlineData("XXX", "XXX")]
    public async Task Missing_currency_returns_404(string from, string to)
    {
        using var app = new ApiFactory();
        using var client = app.CreateAuthorizedClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/currency/convert",
            new { fromCurrencyCode = from, toCurrencyCode = to, amount = 1 })).StatusCode);
    }
}
