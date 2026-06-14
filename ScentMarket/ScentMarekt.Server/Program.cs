using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ScentMarekt.Server.Persistence;
using ScentMarekt.Server.Services;
using ScentMarket.Shared;

var builder = WebApplication.CreateBuilder(args);

// ── Services ────────────────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste your JWT token here. (Do not type 'Bearer', it will be added automatically.)"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<StorageService>();
builder.Services.AddMemoryCache();

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

app.MapGet("/api/perfumes", async (AppDbContext db, IMemoryCache cache, int page = 1, int pageSize = 12, string? search = null) =>
{
    if (!cache.TryGetValue("all_perfumes", out List<Perfume>? allPerfumes))
    {
        allPerfumes = await db.Perfumes
            .AsNoTracking()
            .Select(p => new Perfume
            {
                Id            = p.Id,
                Brand         = p.Brand,
                Name          = p.Name,
                Concentration = p.Concentration,
                ImageUrl      = p.ImageUrl,
                MinPrice      = p.Offers
                    .Where(o => o.IsActive)
                    .SelectMany(o => o.Prices)
                    .Select(pr => (decimal?)pr.Price)
                    .Min()
            })
            .ToListAsync();
        
        cache.Set("all_perfumes", allPerfumes);
    }

    var query = allPerfumes!.AsEnumerable();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var pattern = search.Trim();
        query = query.Where(p =>
            p.Brand.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    var totalCount = query.Count();
    var items = query
        .OrderBy(p => p.Brand)
        .ThenBy(p => p.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToArray();

    return Results.Ok(new PagedResult<Perfume>
    {
        Items = items,
        Page = page,
        PageSize = pageSize,
        TotalCount = totalCount
    });
})
    .WithName("GetPerfumes")
    .WithTags("Perfumes");

app.MapGet("/api/perfumes/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var perfume = await db.Perfumes
        .AsNoTracking()
        .Where(p => p.Id == id)
        .Select(p => new PerfumeDetailDto
        {
            Id            = p.Id,
            Brand         = p.Brand,
            Name          = p.Name,
            Concentration = p.Concentration,
            ImageUrl      = p.ImageUrl,
            Offers        = p.Offers
                .Where(o => o.IsActive)
                .OrderBy(o => o.Prices.Select(pr => (decimal?)pr.Price).Min())
                .Select(o => new OfferListItemDto
                {
                    Id                = o.Id,
                    SellerUsername    = o.Seller.Username,
                    AvailableVolumeMl = o.AvailableVolumeMl,
                    CreatedAt         = o.CreatedAt,
                    ContactInfo       = new ContactInfoDto
                    {
                        Email     = o.Seller.Email,
                        Phone     = o.Seller.Phone,
                        WhatsApp  = o.Seller.WhatsApp,
                        Messenger = o.Seller.Messenger
                    },
                    Prices            = o.Prices
                        .OrderBy(p => p.CapacityMl)
                        .Select(p => new OfferPriceSummary
                        {
                            Id         = p.Id,
                            CapacityMl = p.CapacityMl,
                            Price      = p.Price
                        })
                        .ToList(),
                    SellerAverageRating = Math.Round(
                        o.Seller.ReceivedReviews.Count > 0 
                            ? o.Seller.ReceivedReviews.Average(r => r.Rating) 
                            : 0, 1),
                    SellerReviewCount = o.Seller.ReceivedReviews.Count
                })
                .ToList()
        })
        .FirstOrDefaultAsync();

    return perfume is null ? Results.NotFound() : Results.Ok(perfume);
})
    .WithName("GetPerfumeDetail")
    .WithTags("Perfumes")
    .RequireAuthorization();

app.MapPost("/api/perfumes", async (
    [FromForm] string brand, 
    [FromForm] string name, 
    [FromForm] string concentration, 
    IFormFile image, 
    AppDbContext db, 
    StorageService storage) =>
{
    var objectName = $"{Guid.NewGuid():N}{Path.GetExtension(image.FileName)}";

    using var ms = new MemoryStream();
    await image.CopyToAsync(ms);
    var bytes = ms.ToArray();

    var url = await storage.UploadAsync(objectName, bytes, image.ContentType);

    var perfume = new Perfume
    {
        Id = Guid.NewGuid(),
        Brand = brand,
        Name = name,
        Concentration = concentration,
        ImageUrl = url
    };

    db.Perfumes.Add(perfume);
    await db.SaveChangesAsync();

    return Results.Ok(new { id = perfume.Id, imageUrl = url });
})
.WithName("CreatePerfume")
.WithTags("Perfumes")
.RequireAuthorization(policy => policy.RequireRole("Admin"))
.DisableAntiforgery();

app.MapGet("/api/users/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var user = await db.Users
        .Include(u => u.ReceivedReviews)
            .ThenInclude(r => r.Reviewer)
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.Id == id);
    
    if (user is null) return Results.NotFound();

    var reviewCount = user.ReceivedReviews.Count;
    var averageRating = reviewCount > 0 ? user.ReceivedReviews.Average(r => r.Rating) : 0;

    return Results.Ok(new UserProfileDto
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Phone = user.Phone,
        WhatsApp = user.WhatsApp,
        Messenger = user.Messenger,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt,
        ReviewCount = reviewCount,
        AverageRating = Math.Round(averageRating, 1),
        Reviews = user.ReceivedReviews
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new UserReviewDto
            {
                Rating = r.Rating,
                Comment = r.Comment,
                ReviewerUsername = r.Reviewer.Username,
                CreatedAt = r.CreatedAt
            })
            .ToList()
    });
})
.WithName("GetProfile")
.WithTags("Account")
.RequireAuthorization();

app.MapPut("/api/users/{id:guid}", async (Guid id, UpdateProfileRequest request, ClaimsPrincipal userPrincipal, AppDbContext db) =>
{
    var callerId = Guid.Parse(userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var callerRole = userPrincipal.FindFirstValue(ClaimTypes.Role);

    if (callerRole != "Admin" && callerId != id)
    {
        return Results.Forbid();
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
    
    if (user is null) return Results.NotFound();

    user.Email = request.Email;
    user.Phone = request.Phone;
    user.WhatsApp = request.WhatsApp;
    user.Messenger = request.Messenger;

    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithName("UpdateProfile")
.WithTags("Account")
.RequireAuthorization();

app.MapPut("/api/users/{id:guid}/password", async (Guid id, UpdatePasswordRequest request, ClaimsPrincipal userPrincipal, AppDbContext db) =>
{
    var callerId = Guid.Parse(userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var callerRole = userPrincipal.FindFirstValue(ClaimTypes.Role);

    if (callerRole != "Admin" && callerId != id)
    {
        return Results.Forbid();
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
    
    if (user is null) return Results.NotFound();

    if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
    {
        return Results.BadRequest(new { message = "Incorrect current password." });
    }

    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
    
    await db.SaveChangesAsync();
    return Results.Ok();
})
.WithName("UpdatePassword")
.WithTags("Account")
.RequireAuthorization();

app.MapGet("/api/transactions", async (ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    var transactions = await db.Transactions
        .AsNoTracking()
        .Where(t => t.BuyerId == userId || t.Offer.SellerId == userId)
        .OrderByDescending(t => t.CreatedAt)
        .Select(t => new TransactionDto
        {
            Id = t.Id,
            BuyerId = t.BuyerId,
            BuyerUsername = t.Buyer.Username,
            OfferId = t.OfferId,
            SellerUsername = t.Offer.Seller.Username,
            PerfumeBrand = t.Offer.Perfume.Brand,
            PerfumeName = t.Offer.Perfume.Name,
            PerfumeConcentration = t.Offer.Perfume.Concentration,
            PerfumeImageUrl = t.Offer.Perfume.ImageUrl,
            VolumeBoughtMl = t.VolumeBoughtMl,
            TotalPrice = t.TotalPrice,
            Status = t.Status,
            CreatedAt = t.CreatedAt,
            SellerContactInfo = new ContactInfoDto
            {
                Email = t.Offer.Seller.Email,
                Phone = t.Offer.Seller.Phone,
                WhatsApp = t.Offer.Seller.WhatsApp,
                Messenger = t.Offer.Seller.Messenger
            },
            BuyerContactInfo = new ContactInfoDto
            {
                Email = t.Buyer.Email,
                Phone = t.Buyer.Phone,
                WhatsApp = t.Buyer.WhatsApp,
                Messenger = t.Buyer.Messenger
            },
            ReviewRating = t.Reviews
                .Select(r => (int?)r.Rating)
                .FirstOrDefault(),
            ReviewComment = t.Reviews
                .Select(r => r.Comment)
                .FirstOrDefault()
        })
        .ToListAsync();

    return Results.Ok(transactions);
})
.WithName("GetTransactions")
.WithTags("Transactions")
.RequireAuthorization();

app.MapPost("/api/transactions", async (CreateTransactionRequest request, ClaimsPrincipal user, AppDbContext db) =>
{
    var buyerId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    var offer = await db.Offers
        .Include(o => o.Prices)
        .FirstOrDefaultAsync(o => o.Id == request.OfferId);
        
    if (offer is null || !offer.IsActive)
        return Results.BadRequest(new { message = "Offer is not available." });
        
    if (offer.SellerId == buyerId)
        return Results.BadRequest(new { message = "You cannot buy your own offer." });
        
    var tier = offer.Prices.FirstOrDefault(p => p.CapacityMl == request.VolumeBoughtMl);
    if (tier is null)
        return Results.BadRequest(new { message = "Selected volume is not available for this offer." });
        
    if (offer.AvailableVolumeMl < request.VolumeBoughtMl)
        return Results.BadRequest(new { message = "Not enough volume available." });
        
    var transaction = new Transaction
    {
        Id = Guid.NewGuid(),
        BuyerId = buyerId,
        OfferId = offer.Id,
        VolumeBoughtMl = request.VolumeBoughtMl,
        TotalPrice = tier.Price,
        Status = TransactionStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };
    
    offer.AvailableVolumeMl -= request.VolumeBoughtMl;
    if (offer.AvailableVolumeMl == 0)
    {
        offer.IsActive = false;
    }
    
    db.Transactions.Add(transaction);
    await db.SaveChangesAsync();
    
    return Results.Ok(new { transaction.Id });
})
.WithName("CreateTransaction")
.WithTags("Transactions")
.RequireAuthorization();

app.MapPut("/api/transactions/{id:guid}/status", async (Guid id, UpdateStatusRequest request, ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    
    var transaction = await db.Transactions
        .Include(t => t.Offer)
        .FirstOrDefaultAsync(t => t.Id == id);
        
    if (transaction is null) return Results.NotFound();
    
    bool isBuyer = transaction.BuyerId == userId;
    bool isSeller = transaction.Offer.SellerId == userId;
    
    if (!isBuyer && !isSeller) return Results.Forbid();
    
    if (request.Status == TransactionStatus.Completed && !isSeller)
        return Results.BadRequest(new { message = "Only the seller can confirm the transaction." });
        
    if (request.Status == TransactionStatus.Cancelled && transaction.Status != TransactionStatus.Pending)
        return Results.BadRequest(new { message = "Can only cancel pending transactions." });
        
    if (request.Status == TransactionStatus.Cancelled)
    {
        transaction.Offer.AvailableVolumeMl += transaction.VolumeBoughtMl;
        transaction.Offer.IsActive = true;
    }
    
    transaction.Status = request.Status;
    await db.SaveChangesAsync();
    
    return Results.Ok();
})
.WithName("UpdateTransactionStatus")
.WithTags("Transactions")
.RequireAuthorization();

app.MapPost("/api/reviews", async (CreateReviewRequest request, ClaimsPrincipal user, AppDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var transaction = await db.Transactions
        .Include(t => t.Offer)
        .FirstOrDefaultAsync(t => t.Id == request.TransactionId);

    if (transaction is null)
    {
        return Results.NotFound();
    }

    if (transaction.BuyerId != userId)
    {
        return Results.Forbid();
    }

    if (transaction.Status != TransactionStatus.Completed)
    {
        return Results.BadRequest(new { message = "Can only review completed transactions." });
    }

    var existingReview = await db.Reviews
        .AnyAsync(r => r.TransactionId == request.TransactionId && r.ReviewerId == userId);

    if (existingReview)
    {
        return Results.BadRequest(new { message = "You have already reviewed this transaction." });
    }

    var review = new Review
    {
        TransactionId = request.TransactionId,
        ReviewerId = userId,
        RevieweeId = transaction.Offer.SellerId,
        Rating = request.Rating,
        Comment = request.Comment,
        CreatedAt = DateTime.UtcNow
    };

    db.Reviews.Add(review);
    await db.SaveChangesAsync();

    return Results.Ok();
})
.WithName("CreateReview")
.WithTags("Reviews")
.RequireAuthorization();

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
    .WithName("GetHealth")
    .WithTags("System");

app.MapPost("/api/auth/register", async (RegisterRequest request, AuthService auth) =>
{
    var response = await auth.RegisterAsync(request);
    return response is null
        ? Results.Conflict(new { message = "Username is already taken." })
        : Results.Ok(response);
})
    .WithName("Register")
    .WithTags("Authorization");

app.MapPost("/api/auth/login", async (LoginRequest request, AuthService auth) =>
{
    var response = await auth.LoginAsync(request);
    return response is null
        ? Results.Unauthorized()
        : Results.Ok(response);
})
    .WithName("Login")
    .WithTags("Authorization");

// ── Offers ───────────────────────────────────────────────────────────────────

app.MapGet("/api/users/{id:guid}/offers", async (Guid id, AppDbContext db) =>
{
    var offers = await db.Offers
        .Include(o => o.Prices)
        .Where(o => o.SellerId == id)
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
    .WithName("GetUserOffers")
    .WithTags("Offers");


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
    .WithTags("Offers")
    .RequireAuthorization();

// ── Admin Users ──────────────────────────────────────────────────────────────

app.MapGet("/api/users", async (AppDbContext db) =>
{
    var users = await db.Users
        .AsNoTracking()
        .OrderByDescending(u => u.CreatedAt)
        .Select(u => new AdminUserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        })
        .ToListAsync();

    return Results.Ok(users);
})
    .WithName("GetAllUsers")
    .WithTags("Admin")
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapPost("/api/users", async (AdminCreateUserRequest request, AppDbContext db) =>
{
    if (await db.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
        return Results.Conflict(new { message = "Username or Email already taken." });

    if (!Enum.TryParse<UserRole>(request.Role, true, out var roleEnum))
        return Results.BadRequest(new { message = "Invalid role." });

    var user = new User
    {
        Id = Guid.NewGuid(),
        Username = request.Username,
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Role = roleEnum,
        CreatedAt = DateTime.UtcNow
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new AdminUserDto
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role.ToString(),
        CreatedAt = user.CreatedAt
    });
})
    .WithName("AdminCreateUser")
    .WithTags("Admin")
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapDelete("/api/users/{id:guid}", async (Guid id, ClaimsPrincipal userPrincipal, AppDbContext db) =>
{
    var callerId = Guid.Parse(userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    if (id == callerId)
        return Results.BadRequest(new { message = "You cannot delete yourself." });

    var user = await db.Users.FindAsync(id);
    if (user is null) return Results.NotFound();

    db.Users.Remove(user);
    await db.SaveChangesAsync();

    return Results.Ok();
})
    .WithName("DeleteUser")
    .WithTags("Admin")
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();