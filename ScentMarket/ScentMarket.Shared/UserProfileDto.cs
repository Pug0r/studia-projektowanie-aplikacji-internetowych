namespace ScentMarket.Shared;

public sealed class UserProfileDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? WhatsApp { get; init; }
    public string? Messenger { get; init; }
    public string Role { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
