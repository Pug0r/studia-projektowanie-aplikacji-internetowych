using System.ComponentModel.DataAnnotations.Schema;

namespace ScentMarket.Shared;

public sealed class Perfume
{
	public Guid Id { get; init; }

	public string Brand { get; init; } = string.Empty;

	public string Name { get; init; } = string.Empty;

	public string Concentration { get; init; } = string.Empty;

    public string? ImageUrl { get; set; }

    /// <summary>
    /// Cheapest price across all active offers. Populated by the API — not stored in the DB.
    /// </summary>
    [NotMapped]
    public decimal? MinPrice { get; set; }

    public ICollection<Offer> Offers { get; init; } = new List<Offer>();

	public string Describe() => $"{Id}:{Brand}:{Name}:{Concentration}";
}