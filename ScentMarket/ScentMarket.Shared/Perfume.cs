namespace ScentMarket.Shared;

public sealed class Perfume
{
	public Guid Id { get; init; }

	public string Brand { get; init; } = string.Empty;

	public string Name { get; init; } = string.Empty;

	public string Concentration { get; init; } = string.Empty;

    public string? ImageUrl { get; set; }

    public ICollection<Offer> Offers { get; init; } = new List<Offer>();

	public string Describe() => $"{Id}:{Brand}:{Name}:{Concentration}";
}