namespace ScentMarket.Shared;

public sealed class CreateTransactionRequest
{
    public Guid OfferId { get; init; }
    public int VolumeBoughtMl { get; init; }
}
