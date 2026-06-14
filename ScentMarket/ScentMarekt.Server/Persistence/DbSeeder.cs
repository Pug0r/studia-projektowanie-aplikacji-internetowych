using Microsoft.EntityFrameworkCore;
using ScentMarket.Shared;

namespace ScentMarekt.Server.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Perfumes.AnyAsync())
        {
            return;
        }

        db.Perfumes.AddRange(
            new Perfume
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Brand = "Maison Francis Kurkdjian",
                Name = "Baccarat Rouge 540",
                Concentration = "Extrait de Parfum"
            },
            new Perfume
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Brand = "Creed",
                Name = "Aventus",
                Concentration = "Eau de Parfum"
            },
            new Perfume
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Brand = "Nishane",
                Name = "Hacivat",
                Concentration = "Extrait de Parfum"
            });

        await db.SaveChangesAsync();
    }
}