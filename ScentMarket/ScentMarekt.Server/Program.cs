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

app.Run();

