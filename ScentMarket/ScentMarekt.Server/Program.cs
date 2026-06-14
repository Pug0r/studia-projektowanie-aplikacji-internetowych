using Npgsql;
using ScentMarket.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

var perfumes = new[]
{
    new Perfume { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Maison Francis Kurkdjian - Baccarat Rouge 540" },
    new Perfume { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Creed - Aventus" },
    new Perfume { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Nishane - Hacivat" }
};

app.MapGet("/api/perfumes", () => perfumes)
    .WithName("GetPerfumes");

app.MapGet("/api/db-check", async (IConfiguration configuration) =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand("SELECT 1", connection);
    var result = await command.ExecuteScalarAsync();

    return Results.Ok(new
    {
        connected = true,
        database = connection.Database,
        serverVersion = connection.PostgreSqlVersion.ToString(),
        probe = result
    });
})
    .WithName("CheckDatabaseConnection");

app.Run();

