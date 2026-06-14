using System.Security.Claims;
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

app.MapGet("/api/perfumes", async (AppDbContext db, int page = 1, int pageSize = 12, string? search = null) =>
{
    var query = db.Perfumes.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var pattern = $"%{search.Trim()}%";
        query = query.Where(p =>
            EF.Functions.ILike(p.Brand, pattern) ||
            EF.Functions.ILike(p.Name, pattern));
    }

    var totalCount = await query.CountAsync();
    var items = await query
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

// ── Offers ───────────────────────────────────────────────────────────────────

app.MapGet("/api/offers/my", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var offers = await db.Offers
        .AsNoTracking()
        .Where(o => o.SellerId == userId)
        .Include(o => o.Perfume)
        .Include(o => o.Prices)
        .OrderByDescending(o => o.CreatedAt)
        .Select(o => new MyOfferDto
        {
            Id             = o.Id,
            PerfumeId      = o.PerfumeId,
            PerfumeBrand   = o.Perfume.Brand,
            PerfumeName    = o.Perfume.Name,
            PerfumeImageUrl = o.Perfume.ImageUrl,
            PerfumeConcentration = o.Perfume.Concentration,
            AvailableVolumeMl = o.AvailableVolumeMl,
            IsActive       = o.IsActive,
            CreatedAt      = o.CreatedAt,
            Prices         = o.Prices
                .OrderBy(p => p.CapacityMl)
                .Select(p => new OfferPriceSummary
                {
                    Id         = p.Id,
                    CapacityMl = p.CapacityMl,
                    Price      = p.Price
                })
                .ToList()
        })
        .ToListAsync();

    return Results.Ok(offers);
})
    .WithName("GetMyOffers")
    .RequireAuthorization();

app.MapPost("/api/offers", async (ClaimsPrincipal user, AppDbContext db, CreateOfferRequest request) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Guard: token may be stale if the DB was reset
    var userExists = await db.Users.AnyAsync(u => u.Id == userId);
    if (!userExists)
        return Results.Unauthorized();

    var perfume = await db.Perfumes.FindAsync(request.PerfumeId);
    if (perfume is null)
        return Results.NotFound(new { message = "Perfume not found." });

    if (request.Prices is null || request.Prices.Count == 0)
        return Results.BadRequest(new { message = "At least one price tier is required." });

    if (request.AvailableVolumeMl <= 0)
        return Results.BadRequest(new { message = "Available volume must be greater than 0." });

    var offerId = Guid.NewGuid();
    var offer = new Offer
    {
        Id                = offerId,
        SellerId          = userId,
        PerfumeId         = request.PerfumeId,
        AvailableVolumeMl = request.AvailableVolumeMl,
        IsActive          = true,
        CreatedAt         = DateTime.UtcNow,
        Prices            = request.Prices
            .Where(p => p.CapacityMl > 0 && p.Price > 0)
            .Select(p => new OfferPrice
            {
                Id         = Guid.NewGuid(),
                OfferId    = offerId,
                CapacityMl = p.CapacityMl,
                Price      = p.Price
            })
            .ToList()
    };

    if (offer.Prices.Count == 0)
        return Results.BadRequest(new { message = "All price tiers must have capacity and price > 0." });

    db.Offers.Add(offer);
    await db.SaveChangesAsync();

    var dto = new MyOfferDto
    {
        Id                   = offer.Id,
        PerfumeId            = perfume.Id,
        PerfumeBrand         = perfume.Brand,
        PerfumeName          = perfume.Name,
        PerfumeImageUrl      = perfume.ImageUrl,
        PerfumeConcentration = perfume.Concentration,
        AvailableVolumeMl    = offer.AvailableVolumeMl,
        IsActive             = offer.IsActive,
        CreatedAt            = offer.CreatedAt,
        Prices               = offer.Prices
            .OrderBy(p => p.CapacityMl)
            .Select(p => new OfferPriceSummary { Id = p.Id, CapacityMl = p.CapacityMl, Price = p.Price })
            .ToList()
    };

    return Results.Ok(dto);
})
    .WithName("CreateOffer")
    .RequireAuthorization();

app.Run();