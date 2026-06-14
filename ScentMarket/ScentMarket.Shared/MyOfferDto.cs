namespace ScentMarket.Shared;

/// <summary>Offer returned from the API — flat projection to avoid circular references.</summary>
public sealed class MyOfferDto
{
    public Guid Id { get; init; }

    public Guid PerfumeId { get; init; }
    public string PerfumeBrand { get; init; } = string.Empty;
    public string PerfumeName { get; init; } = string.Empty;
    public string? PerfumeImageUrl { get; init; }
    public string PerfumeConcentration { get; init; } = string.Empty;

    public int AvailableVolumeMl { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

    public List<OfferPriceSummary> Prices { get; init; } = [];
}
