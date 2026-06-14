namespace ScentMarket.Shared;

public sealed class Transaction
{
    public Guid Id { get; init; }

    public Guid BuyerId { get; init; }

    public User Buyer { get; init; } = null!;

    public Guid OfferId { get; init; }

    public Offer Offer { get; init; } = null!;

    public int VolumeBoughtMl { get; init; }

    public decimal TotalPrice { get; init; }

    public TransactionStatus Status { get; set; }

    public DateTime CreatedAt { get; init; }

    public ICollection<Review> Reviews { get; init; } = new List<Review>();

    public string Describe() => $"{Id}:{BuyerId}:{OfferId}:{VolumeBoughtMl}:{TotalPrice}:{Status}:{CreatedAt:o}:{Reviews.Count}";
}
