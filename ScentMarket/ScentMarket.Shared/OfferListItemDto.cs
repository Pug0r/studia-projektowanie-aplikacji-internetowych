namespace ScentMarket.Shared;

/// <summary>Single offer shown on a perfume detail page — hides sensitive seller data.</summary>
public sealed class OfferListItemDto
{
    public Guid Id { get; init; }
    public string SellerUsername { get; init; } = string.Empty;
    public int AvailableVolumeMl { get; init; }
    public DateTime CreatedAt { get; init; }
    public ContactInfoDto ContactInfo { get; init; } = new();
    public List<OfferPriceSummary> Prices { get; init; } = [];
    public double SellerAverageRating { get; init; }
    public int SellerReviewCount { get; init; }
}
