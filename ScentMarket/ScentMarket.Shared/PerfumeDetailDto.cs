namespace ScentMarket.Shared;

/// <summary>Full perfume detail — perfume info plus all active offers from every seller.</summary>
public sealed class PerfumeDetailDto
{
    public Guid Id { get; init; }
    public string Brand { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Concentration { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }

    public List<OfferListItemDto> Offers { get; init; } = [];
}
