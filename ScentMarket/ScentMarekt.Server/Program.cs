using ScentMarket.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var perfumes = new[]
{
    new Perfume { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Maison Francis Kurkdjian - Baccarat Rouge 540" },
    new Perfume { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Creed - Aventus" },
    new Perfume { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Nishane - Hacivat" }
};

app.MapGet("/api/perfumes", () => perfumes)
    .WithName("GetPerfumes");

app.Run();

