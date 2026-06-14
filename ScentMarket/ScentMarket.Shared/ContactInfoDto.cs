namespace ScentMarket.Shared;

public sealed class ContactInfoDto
{
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? WhatsApp { get; set; }
    public string? Messenger { get; set; }
}
