namespace ScentMarket.Shared;

public sealed class CreateOfferRequest
{
    public Guid PerfumeId { get; set; }

    /// <summary>Total volume the seller has available (e.g. 100 ml of a full bottle).</summary>
    public int AvailableVolumeMl { get; set; }

    /// <summary>At least one price tier required.</summary>
    public List<OfferPriceInput> Prices { get; set; } = [];
}
