namespace ScentMarket.Shared;

public sealed class UserReviewDto
{
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public string ReviewerUsername { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
