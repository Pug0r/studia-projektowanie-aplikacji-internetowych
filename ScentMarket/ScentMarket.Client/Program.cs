using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ScentMarket.Client;
using ScentMarket.Client.Http;
using ScentMarket.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// LocalStorage + custom auth state provider
builder.Services.AddSingleton<LocalStorageService>();
builder.Services.AddSingleton<AuthStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AuthStateProvider>());

// Blazor auth cascade
builder.Services.AddAuthorizationCore();

// Typed HTTP client — same-origin so nginx proxies /api/* to backend
builder.Services.AddHttpClient<ApiClient>(client =>
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

await builder.Build().RunAsync();