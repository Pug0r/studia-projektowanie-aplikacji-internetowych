using System.Net.Http.Json;
using ScentMarket.Shared;

namespace ScentMarket.Client.Http;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<Perfume[]?> GetPerfumesAsync()
    {
        return await _http.GetFromJsonAsync<Perfume[]>("api/perfumes");
    }

    public async Task<BackendHealth?> GetHealthAsync()
    {
        return await _http.GetFromJsonAsync<BackendHealth>("health");
    }
}