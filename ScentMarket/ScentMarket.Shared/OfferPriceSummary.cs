namespace ScentMarket.Shared;

public sealed class OfferPriceSummary
{
    public Guid Id { get; init; }
    public int CapacityMl { get; init; }
    public decimal Price { get; init; }
}
