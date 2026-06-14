using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ScentMarekt.Server.Persistence;
using ScentMarekt.Server.Services;
using ScentMarket.Shared;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<StorageService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ── App ──────────────────────────────────────────────────────────────────────

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var storage = scope.ServiceProvider.GetRequiredService<StorageService>();
    await DbSeeder.SeedAsync(db, storage);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ScentMarket API V1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints ────────────────────────────────────────────────────────────────

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapGet("/api/perfumes", async (AppDbContext db, int page = 1, int pageSize = 12) =>
{
    var totalCount = await db.Perfumes.CountAsync();
    var items = await db.Perfumes
        .AsNoTracking()
        .OrderBy(p => p.Brand)
        .ThenBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToArrayAsync();

    return Results.Ok(new PagedResult<Perfume>
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    });
})
    .WithName("GetPerfumes");

app.MapGet("/health", async (AppDbContext db) =>
{
    try
    {
        await db.Database.OpenConnectionAsync();
        var connection = db.Database.GetDbConnection();

        return Results.Ok(new BackendHealth
        {
            Healthy = true,
            Message = "Database is reachable.",
            Database = connection.Database,
            ServerVersion = connection.ServerVersion
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new BackendHealth
        {
            Healthy = false,
            Message = $"Database unavailable: {ex.Message}"
        });
    }
    finally
    {
        await db.Database.CloseConnectionAsync();
    }
})
    .WithName("GetHealth");

app.MapPost("/api/auth/register", async (RegisterRequest request, AuthService auth) =>
{
    var response = await auth.RegisterAsync(request);
    return response is null
        ? Results.Conflict(new { message = "Username is already taken." })
        : Results.Ok(response);
})
    .WithName("Register");

app.MapPost("/api/auth/login", async (LoginRequest request, AuthService auth) =>
{
    var response = await auth.LoginAsync(request);
    return response is null
        ? Results.Unauthorized()
        : Results.Ok(response);
})
    .WithName("Login");

app.Run();