namespace Clt.Api.Domain.Entities;

public sealed class Currency
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal RateToBase { get; set; }
}
