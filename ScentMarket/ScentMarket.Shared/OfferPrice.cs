using System.ComponentModel.DataAnnotations.Schema;

namespace ScentMarket.Shared;

public sealed class OfferPrice
{
    public Guid Id { get; init; }

    public Guid OfferId { get; init; }

    public Offer Offer { get; init; } = null!;

    public int CapacityMl { get; init; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Price { get; init; }

    public string Describe() => $"{Id}:{OfferId}:{CapacityMl}:{Price}";
}