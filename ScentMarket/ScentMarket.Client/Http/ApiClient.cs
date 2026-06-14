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

    public async Task<PagedResult<Perfume>?> GetPerfumesAsync(int page = 1, int pageSize = 12, string? search = null)
    {
        await AttachTokenAsync();
        var url = $"api/perfumes?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        return await _http.GetFromJsonAsync<PagedResult<Perfume>>(url);
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