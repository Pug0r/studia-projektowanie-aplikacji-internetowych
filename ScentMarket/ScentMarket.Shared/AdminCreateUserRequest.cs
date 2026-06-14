using System.ComponentModel.DataAnnotations;

namespace ScentMarket.Shared;

public sealed class AdminCreateUserRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "User";
}
