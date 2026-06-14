using System.Net.Http.Json;
using ScentMarket.Client.Services;
using ScentMarket.Shared;

namespace ScentMarket.Client.Http;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public ApiClient(HttpClient http, AuthStateProvider authState)
    {
        _http = http;
        _authState = authState;
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<AuthResponse>()
            : null;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<AuthResponse>()
            : null;
    }

    // ── Perfumes ─────────────────────────────────────────────────────────────

    public async Task<Perfume[]?> GetPerfumesAsync()
    {
        await AttachTokenAsync();
        return await _http.GetFromJsonAsync<Perfume[]>("api/perfumes");
    }

    // ── Health ───────────────────────────────────────────────────────────────

    public async Task<BackendHealth?> GetHealthAsync()
    {
        return await _http.GetFromJsonAsync<BackendHealth>("health");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task AttachTokenAsync()
    {
        var token = await _authState.GetTokenAsync();
        _http.DefaultRequestHeaders.Authorization = token is not null
            ? new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token)
            : null;
    }
}