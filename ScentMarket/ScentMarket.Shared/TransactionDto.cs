namespace ScentMarket.Shared;

public sealed class TransactionDto
{
    public Guid Id { get; init; }
    public Guid BuyerId { get; init; }
    public string BuyerUsername { get; init; } = string.Empty;
    public Guid OfferId { get; init; }
    public string SellerUsername { get; init; } = string.Empty;
    public string PerfumeBrand { get; init; } = string.Empty;
    public string PerfumeName { get; init; } = string.Empty;
    public string PerfumeConcentration { get; init; } = string.Empty;
    public string? PerfumeImageUrl { get; init; }
    public int VolumeBoughtMl { get; init; }
    public decimal TotalPrice { get; init; }
    public TransactionStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public ContactInfoDto SellerContactInfo { get; set; } = new();
    public ContactInfoDto BuyerContactInfo { get; set; } = new();
}
