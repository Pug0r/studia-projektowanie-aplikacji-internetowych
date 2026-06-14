namespace ScentMarket.Shared;

public sealed class PagedResult<T>
{
    public required T[] Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public bool HasMore => (long)Page * PageSize < TotalCount;
}
