using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace ScentMarket.Client.Services;

/// <summary>
/// Reads the JWT stored in localStorage, decodes its payload,
/// and surfaces the claims as a Blazor AuthenticationState.
/// </summary>
public sealed class AuthStateProvider : AuthenticationStateProvider
{
    private const string TokenKey = "auth_token";
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private readonly LocalStorageService _storage;

    public AuthStateProvider(LocalStorageService storage) => _storage = storage;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _storage.GetItemAsync(TokenKey);

        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    /// <summary>Called after login/register to push the new state to Blazor.</summary>
    public async Task NotifyUserLoggedInAsync(string token)
    {
        await _storage.SetItemAsync(TokenKey, token);
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        var state = new AuthenticationState(new ClaimsPrincipal(identity));
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    /// <summary>Called on logout to clear state.</summary>
    public async Task NotifyUserLoggedOutAsync()
    {
        await _storage.RemoveItemAsync(TokenKey);
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    /// <summary>Returns the raw token string or null.</summary>
    public Task<string?> GetTokenAsync() => _storage.GetItemAsync(TokenKey).AsTask();

    // ── JWT helpers ──────────────────────────────────────────────────────────

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var json = Base64UrlDecode(payload);

        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                            ?? [];

        return keyValuePairs.SelectMany(kvp => ExtractClaims(kvp.Key, kvp.Value));
    }

    private static IEnumerable<Claim> ExtractClaims(string key, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
                yield return new Claim(key, item.ToString());
        }
        else
        {
            yield return new Claim(key, value.ToString());
        }
    }

    private static string Base64UrlDecode(string base64Url)
    {
        // Pad to a multiple of 4
        var padded = base64Url.PadRight(base64Url.Length + (4 - base64Url.Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
