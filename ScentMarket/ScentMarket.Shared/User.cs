namespace ScentMarket.Shared;

public sealed class User
{
    public Guid Id { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public DateTime CreatedAt { get; init; }

    public ICollection<Offer> SoldOffers { get; init; } = new List<Offer>();

    public ICollection<Transaction> BoughtTransactions { get; init; } = new List<Transaction>();

    public ICollection<Review> WrittenReviews { get; init; } = new List<Review>();

    public ICollection<Review> ReceivedReviews { get; init; } = new List<Review>();

    public string Describe() => $"{Id}:{Username}:{Email}:{Role}:{CreatedAt:o}:{SoldOffers.Count}:{BoughtTransactions.Count}:{WrittenReviews.Count}:{ReceivedReviews.Count}";
}
