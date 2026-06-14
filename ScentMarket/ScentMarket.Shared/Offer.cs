namespace ScentMarket.Shared;

public sealed class Offer
{
    public Guid Id { get; init; }

    public Guid SellerId { get; init; }

    public User Seller { get; init; } = null!;

    public Guid PerfumeId { get; init; }

    public Perfume Perfume { get; init; } = null!;

    public int AvailableVolumeMl { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; init; }

    public ICollection<OfferPrice> Prices { get; init; } = new List<OfferPrice>();

    public ICollection<Transaction> Transactions { get; init; } = new List<Transaction>();

    public string Describe() => $"{Id}:{SellerId}:{PerfumeId}:{AvailableVolumeMl}:{IsActive}:{CreatedAt:o}:{Prices.Count}:{Transactions.Count}";
}
