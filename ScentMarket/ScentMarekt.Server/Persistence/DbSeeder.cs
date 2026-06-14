using System.Text;
using Microsoft.EntityFrameworkCore;
using ScentMarket.Shared;
using ScentMarekt.Server.Services;

namespace ScentMarekt.Server.Persistence;

public static class DbSeeder
{
    // Accent colours per perfume (used in the SVG placeholder)
    private static readonly (string Id, string Brand, string Name, string Concentration, string Accent)[] SeedData =
    [
        ("11111111-1111-1111-1111-111111111111",
            "Maison Francis Kurkdjian", "Baccarat Rouge 540", "Extrait de Parfum", "#c0392b"),

        ("22222222-2222-2222-2222-222222222222",
            "Creed", "Aventus", "Eau de Parfum", "#27ae60"),

        ("33333333-3333-3333-3333-333333333333",
            "Nishane", "Hacivat", "Extrait de Parfum", "#2980b9"),
    ];

    public static async Task SeedAsync(AppDbContext db, StorageService storage)
    {
        // 1. Apply any pending EF migrations
        await db.Database.MigrateAsync();

        // 2. Ensure the S3 bucket exists (idempotent)
        await storage.EnsureBucketAsync();

        // 3. Seed perfumes — skip if already seeded
        if (await db.Perfumes.AnyAsync())
        {
            // Perfumes already exist; backfill images if column was added later
            await BackfillImagesAsync(db, storage);
            return;
        }

        foreach (var seed in SeedData)
        {
            var imageUrl = await UploadPlaceholderAsync(storage, seed.Brand, seed.Name, seed.Accent);

            db.Perfumes.Add(new Perfume
            {
                Id            = Guid.Parse(seed.Id),
                Brand         = seed.Brand,
                Name          = seed.Name,
                Concentration = seed.Concentration,
                ImageUrl      = imageUrl
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// If perfumes exist but have no ImageUrl (seeded before S3 was added),
    /// generate and upload placeholders for them.
    /// </summary>
    private static async Task BackfillImagesAsync(AppDbContext db, StorageService storage)
    {
        var perfumesWithoutImage = await db.Perfumes
            .Where(p => p.ImageUrl == null)
            .ToListAsync();

        if (perfumesWithoutImage.Count == 0) return;

        foreach (var perfume in perfumesWithoutImage)
        {
            var seed = SeedData.FirstOrDefault(s => s.Id == perfume.Id.ToString());
            var accent = seed.Accent ?? "#b48c3c";
            perfume.ImageUrl = await UploadPlaceholderAsync(storage, perfume.Brand, perfume.Name, accent);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Generates an SVG placeholder image for a perfume and uploads it to S3.
    /// Returns the public URL.
    /// </summary>
    private static async Task<string> UploadPlaceholderAsync(
        StorageService storage, string brand, string name, string accent)
    {
        var svg = GenerateSvg(brand, name, accent);
        var bytes = Encoding.UTF8.GetBytes(svg);
        var objectName = $"{Slugify(brand)}-{Slugify(name)}.svg";
        return await storage.UploadAsync(objectName, bytes, "image/svg+xml");
    }

    private static string GenerateSvg(string brand, string name, string accentColor)
    {
        var safeBrand = Escape(brand.ToUpperInvariant());
        var safeName  = Escape(name);

        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 400 533" width="400" height="533">
              <rect width="400" height="533" fill="#1a1a1a"/>
              <radialGradient id="glow" cx="50%" cy="42%" r="45%">
                <stop offset="0%" stop-color="{accentColor}" stop-opacity="0.18"/>
                <stop offset="100%" stop-color="#1a1a1a" stop-opacity="0"/>
              </radialGradient>
              <rect width="400" height="533" fill="url(#glow)"/>
              <circle cx="200" cy="220" r="100" fill="none" stroke="{accentColor}" stroke-width="1" opacity="0.2"/>
              <circle cx="200" cy="220" r="72"  fill="none" stroke="{accentColor}" stroke-width="1" opacity="0.15"/>
              <g transform="translate(200,220)" fill="{accentColor}" opacity="0.25">
                <rect x="-18" y="-70" width="36" height="10" rx="3"/>
                <rect x="-28" y="-60" width="56" height="4" rx="2"/>
                <rect x="-34" y="-56" width="68" height="100" rx="8"/>
                <rect x="-22" y="44"  width="44" height="12" rx="4"/>
              </g>
              <rect x="0" y="490" width="400" height="43" fill="{accentColor}" opacity="0.15"/>
              <rect x="0" y="490" width="400" height="2"  fill="{accentColor}" opacity="0.5"/>
              <text x="200" y="435"
                    font-family="'Helvetica Neue',Helvetica,Arial,sans-serif"
                    font-size="11" font-weight="700"
                    text-anchor="middle" fill="{accentColor}"
                    letter-spacing="3">{safeBrand}</text>
              <text x="200" y="458"
                    font-family="'Helvetica Neue',Helvetica,Arial,sans-serif"
                    font-size="13" font-weight="400"
                    text-anchor="middle" fill="#cccccc">{safeName}</text>
            </svg>
            """;
    }

    private static string Slugify(string s) =>
        s.ToLowerInvariant().Replace(" ", "-").Replace("'", "").Replace(".", "");

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}