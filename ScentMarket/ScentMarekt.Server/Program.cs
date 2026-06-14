using Microsoft.EntityFrameworkCore;
using ScentMarekt.Server.Persistence;
using ScentMarket.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// Configure the HTTP request pipeline.
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

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapGet("/api/perfumes", async (AppDbContext db) =>
    await db.Perfumes
        .AsNoTracking()
        .OrderBy(perfume => perfume.Brand)
        .ThenBy(perfume => perfume.Name)
        .ToArrayAsync())
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

app.Run();