namespace ScentMarket.Shared;

public sealed class OfferPriceInput
{
    /// <summary>Decant size in ml being offered at this price (e.g. 5, 10, 20).</summary>
    public int CapacityMl { get; set; }

    /// <summary>Price for this decant size.</summary>
    public decimal Price { get; set; }
}
