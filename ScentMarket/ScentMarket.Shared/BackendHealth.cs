namespace ScentMarket.Shared;

public sealed class BackendHealth
{
    public bool Healthy { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? Database { get; init; }

    public string? ServerVersion { get; init; }

    public int? CacheItemCount { get; init; }

    public string Summary() => $"{Healthy}: {Message} {Database} {ServerVersion} Cache: {CacheItemCount}";
}


