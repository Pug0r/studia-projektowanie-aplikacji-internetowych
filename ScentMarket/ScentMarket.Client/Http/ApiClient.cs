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

    /// <summary>Fetches all perfumes in one page — used for dropdowns.</summary>
    public async Task<List<Perfume>> GetAllPerfumesAsync()
    {
        await AttachTokenAsync();
        var result = await _http.GetFromJsonAsync<PagedResult<Perfume>>("api/perfumes?page=1&pageSize=1000");
        return result?.Items.ToList() ?? [];
    }

    public async Task<PerfumeDetailDto?> GetPerfumeDetailAsync(Guid id)
    {
        await AttachTokenAsync();
        return await _http.GetFromJsonAsync<PerfumeDetailDto>($"api/perfumes/{id}");
    }

    public async Task<(bool success, string? error)> CreatePerfumeAsync(string brand, string name, string concentration, Microsoft.AspNetCore.Components.Forms.IBrowserFile image)
    {
        await AttachTokenAsync();
        
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(brand), "brand");
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent(concentration), "concentration");

        var stream = image.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10 MB max
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
        content.Add(streamContent, "image", image.Name);

        var response = await _http.PostAsync("api/perfumes", content);

        if (response.IsSuccessStatusCode)
            return (true, null);

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var msg = problem.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            return (false, msg);
        }
        catch
        {
            return (false, response.ReasonPhrase);
        }
    }

    // ── Offers ───────────────────────────────────────────────────────────────

    public async Task<List<MyOfferDto>> GetMyOffersAsync()
    {
        var userId = await GetMyUserIdAsync();
        if (userId == null) return [];
        await AttachTokenAsync();
        return await _http.GetFromJsonAsync<List<MyOfferDto>>($"api/users/{userId}/offers") ?? [];
    }

    public async Task<(MyOfferDto? dto, string? error)> CreateOfferAsync(CreateOfferRequest request)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("api/offers", request);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<MyOfferDto>(), null);

        // Try to extract error message from JSON body
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var msg = problem.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            return (null, msg);
        }
        catch
        {
            return (null, response.ReasonPhrase);
        }
    }

    // ── Health ───────────────────────────────────────────────────────────────

    public async Task<BackendHealth?> GetHealthAsync()
    {
        return await _http.GetFromJsonAsync<BackendHealth>("health");
    }

    // ── Transactions ─────────────────────────────────────────────────────────

    public async Task<List<TransactionDto>> GetTransactionsAsync()
    {
        await AttachTokenAsync();
        var result = await _http.GetFromJsonAsync<List<TransactionDto>>("api/transactions");
        return result ?? [];
    }

    public async Task<(Guid? id, string? error)> CreateTransactionAsync(CreateTransactionRequest request)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("api/transactions", request);
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            return (result.GetProperty("id").GetGuid(), null);
        }
        
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var msg = problem.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            return (null, msg);
        }
        catch
        {
            return (null, response.ReasonPhrase);
        }
    }

    public async Task<bool> UpdateTransactionStatusAsync(Guid id, UpdateStatusRequest request)
    {
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync($"api/transactions/{id}/status", request);
        return response.IsSuccessStatusCode;
    }

    // ── Reviews ──────────────────────────────────────────────────────────────

    public async Task<(bool success, string? error)> SubmitReviewAsync(CreateReviewRequest request)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("api/reviews", request);
        
        if (response.IsSuccessStatusCode)
            return (true, null);
            
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var msg = problem.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            return (false, msg);
        }
        catch
        {
            return (false, response.ReasonPhrase);
        }
    }

    // ── Profile ──────────────────────────────────────────────────────────────

    public async Task<UserProfileDto?> GetProfileAsync()
    {
        var userId = await GetMyUserIdAsync();
        if (userId == null) return null;
        await AttachTokenAsync();
        return await _http.GetFromJsonAsync<UserProfileDto>($"api/users/{userId}");
    }

    public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var userId = await GetMyUserIdAsync();
        if (userId == null) return false;
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync($"api/users/{userId}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<(bool success, string? error)> UpdatePasswordAsync(UpdatePasswordRequest request)
    {
        var userId = await GetMyUserIdAsync();
        if (userId == null) return (false, "Not authenticated.");
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync($"api/users/{userId}/password", request);
        
        if (response.IsSuccessStatusCode)
            return (true, null);
            
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var msg = problem.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            return (false, msg);
        }
        catch
        {
            return (false, response.ReasonPhrase);
        }
    }

    // ── Admin Users ──────────────────────────────────────────────────────────

    public async Task<List<AdminUserDto>> GetUsersAsync()
    {
        await AttachTokenAsync();
        return await _http.GetFromJsonAsync<List<AdminUserDto>>("api/users") ?? [];
    }

    public async Task<(bool success, string? error)> CreateUserAsync(AdminCreateUserRequest request)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync("api/users", request);
        
        if (response.IsSuccessStatusCode)
            return (true, null);
            
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var msg = problem.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            return (false, msg);
        }
        catch
        {
            return (false, response.ReasonPhrase);
        }
    }

    public async Task<(bool success, string? error)> DeleteUserAsync(Guid id)
    {
        await AttachTokenAsync();
        var response = await _http.DeleteAsync($"api/users/{id}");
        
        if (response.IsSuccessStatusCode)
            return (true, null);

        try
        {
            var problem = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            var msg = problem.TryGetProperty("message", out var m) ? m.GetString() : response.ReasonPhrase;
            return (false, msg);
        }
        catch
        {
            return (false, response.ReasonPhrase);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<Guid?> GetMyUserIdAsync()
    {
        var authState = await _authState.GetAuthenticationStateAsync();
        var sub = authState.User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }


    private async Task AttachTokenAsync()
    {
        var token = await _authState.GetTokenAsync();
        _http.DefaultRequestHeaders.Authorization = token is not null
            ? new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token)
            : null;
    }
}