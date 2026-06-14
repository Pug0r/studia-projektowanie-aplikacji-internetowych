namespace ScentMarket.Shared;

public sealed class Review
{
    public Guid Id { get; init; }

    public Guid TransactionId { get; init; }

    public Transaction Transaction { get; init; } = null!;

    public Guid ReviewerId { get; init; }

    public User Reviewer { get; init; } = null!;

    public Guid RevieweeId { get; init; }

    public User Reviewee { get; init; } = null!;

    public int Rating { get; init; }

    public string Comment { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public string Describe() => $"{Id}:{TransactionId}:{ReviewerId}:{RevieweeId}:{Rating}:{CreatedAt:o}:{Comment}";
}
