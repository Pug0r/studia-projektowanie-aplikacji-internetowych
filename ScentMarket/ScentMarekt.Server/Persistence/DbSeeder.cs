using System.Text;
using Microsoft.EntityFrameworkCore;
using ScentMarket.Shared;
using ScentMarekt.Server.Services;

namespace ScentMarekt.Server.Persistence;

public static class DbSeeder
{
    private static readonly (string Id, string Brand, string Name, string Concentration, string Accent)[] SeedData =
    [
        // ── Already seeded ────────────────────────────────────────────────────
        ("11111111-1111-1111-1111-111111111111",
            "Maison Francis Kurkdjian", "Baccarat Rouge 540", "Extrait de Parfum", "#c0392b"),
        ("22222222-2222-2222-2222-222222222222",
            "Creed", "Aventus", "Eau de Parfum", "#27ae60"),
        ("33333333-3333-3333-3333-333333333333",
            "Nishane", "Hacivat", "Extrait de Parfum", "#2980b9"),

        // ── Tom Ford ──────────────────────────────────────────────────────────
        ("44444444-4444-4444-4444-444444444444",
            "Tom Ford", "Black Orchid", "Eau de Parfum", "#7d3c98"),
        ("55555555-5555-5555-5555-555555555555",
            "Tom Ford", "Tobacco Vanille", "Eau de Parfum", "#d35400"),
        ("66666666-6666-6666-6666-666666666666",
            "Tom Ford", "Oud Wood", "Eau de Parfum", "#6e5200"),
        ("77777777-7777-7777-7777-777777777777",
            "Tom Ford", "Neroli Portofino", "Eau de Parfum", "#1a8a5a"),

        // ── Chanel ────────────────────────────────────────────────────────────
        ("88888888-8888-8888-8888-888888888888",
            "Chanel", "No. 5", "Eau de Parfum", "#c8a415"),
        ("99999999-9999-9999-9999-999999999999",
            "Chanel", "Bleu de Chanel", "Eau de Parfum", "#1a5276"),
        ("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            "Chanel", "Coco Mademoiselle", "Eau de Parfum", "#cb4335"),

        // ── Dior ──────────────────────────────────────────────────────────────
        ("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            "Dior", "Sauvage", "Eau de Parfum", "#2471a3"),
        ("cccccccc-cccc-cccc-cccc-cccccccccccc",
            "Dior", "Fahrenheit", "Eau de Toilette", "#a93226"),
        ("dddddddd-dddd-dddd-dddd-dddddddddddd",
            "Dior", "J'adore", "Eau de Parfum", "#d4ac0d"),

        // ── Hermès ────────────────────────────────────────────────────────────
        ("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee",
            "Hermès", "Terre d'Hermès", "Eau de Parfum", "#e67e22"),
        ("ffffffff-ffff-ffff-ffff-ffffffffffff",
            "Hermès", "H24", "Eau de Parfum", "#117a65"),

        // ── Byredo ────────────────────────────────────────────────────────────
        ("12121212-1212-1212-1212-121212121212",
            "Byredo", "Gypsy Water", "Eau de Parfum", "#7f8c8d"),
        ("13131313-1313-1313-1313-131313131313",
            "Byredo", "Bal d'Afrique", "Eau de Parfum", "#ba4a00"),

        // ── Le Labo ───────────────────────────────────────────────────────────
        ("14141414-1414-1414-1414-141414141414",
            "Le Labo", "Santal 33", "Eau de Parfum", "#a04000"),
        ("15151515-1515-1515-1515-151515151515",
            "Le Labo", "Another 13", "Eau de Parfum", "#616a6b"),

        // ── Amouage ───────────────────────────────────────────────────────────
        ("16161616-1616-1616-1616-161616161616",
            "Amouage", "Interlude Man", "Eau de Parfum", "#6e2f1a"),
        ("17171717-1717-1717-1717-171717171717",
            "Amouage", "Reflection Man", "Eau de Parfum", "#1b4f72"),

        // ── Frederic Malle ────────────────────────────────────────────────────
        ("18181818-1818-1818-1818-181818181818",
            "Frederic Malle", "Portrait of a Lady", "Eau de Parfum", "#922b21"),
        ("19191919-1919-1919-1919-191919191919",
            "Frederic Malle", "Carnal Flower", "Eau de Parfum", "#1a8a5a"),

        // ── Xerjoff ───────────────────────────────────────────────────────────
        ("21212121-2121-2121-2121-212121212121",
            "Xerjoff", "Naxos", "Eau de Parfum", "#1a8a5a"),

        // ── Serge Lutens ──────────────────────────────────────────────────────
        ("23232323-2323-2323-2323-232323232323",
            "Serge Lutens", "Chergui", "Eau de Parfum", "#d68910"),

        // ── Acqua di Parma ────────────────────────────────────────────────────
        ("24242424-2424-2424-2424-242424242424",
            "Acqua di Parma", "Colonia", "Eau de Cologne", "#e8935a"),

        // ── Guerlain ──────────────────────────────────────────────────────────
        ("25252525-2525-2525-2525-252525252525",
            "Guerlain", "Shalimar", "Eau de Parfum", "#8e44ad"),
        ("26262626-2626-2626-2626-262626262626",
            "Guerlain", "L'Homme Idéal", "Eau de Parfum", "#1a5276"),

        // ── Paco Rabanne ──────────────────────────────────────────────────────
        ("27272727-2727-2727-2727-272727272727",
            "Paco Rabanne", "1 Million", "Eau de Toilette", "#d4ac0d"),

        // ── Viktor & Rolf ─────────────────────────────────────────────────────
        ("28282828-2828-2828-2828-282828282828",
            "Viktor & Rolf", "Spicebomb", "Eau de Toilette", "#7b241c"),

        // ── Maison Margiela ───────────────────────────────────────────────────
        ("29292929-2929-2929-2929-292929292929",
            "Maison Margiela", "Replica Jazz Club", "Eau de Toilette", "#784212"),
        ("30303030-3030-3030-3030-303030303030",
            "Maison Margiela", "Replica By the Fireplace", "Eau de Toilette", "#cb4335"),

        // ── Initio ────────────────────────────────────────────────────────────
        ("31313131-3131-3131-3131-313131313131",
            "Initio Parfums", "Oud for Greatness", "Eau de Parfum", "#1b2631"),

        // ── Roja Parfums ──────────────────────────────────────────────────────
        ("32323232-3232-3232-3232-323232323232",
            "Roja Parfums", "Enigma", "Parfum", "#6c3483"),

        // ── Penhaligon's ──────────────────────────────────────────────────────
        ("33333334-3333-3333-3333-333333333334",
            "Penhaligon's", "The Tragedy of Lord George", "Eau de Parfum", "#145a32"),

        // ── Giorgio Armani ────────────────────────────────────────────────────
        ("34343434-3434-3434-3434-343434343434",
            "Giorgio Armani", "Acqua di Gio Profondo", "Eau de Parfum", "#117a65"),
    ];

    public static async Task SeedAsync(AppDbContext db, StorageService storage)
    {
        // 1. Apply any pending EF migrations
        await db.Database.MigrateAsync();

        // 2. Ensure the S3 bucket exists
        await storage.EnsureBucketAsync();

        // 3. Incremental seeding — add any entry from SeedData not yet in DB
        var existingIds = (await db.Perfumes.Select(p => p.Id).ToListAsync()).ToHashSet();

        foreach (var seed in SeedData)
        {
            var id = Guid.Parse(seed.Id);
            if (existingIds.Contains(id)) continue;

            var imageUrl = await UploadPlaceholderAsync(storage, seed.Brand, seed.Name, seed.Accent);

            db.Perfumes.Add(new Perfume
            {
                Id            = id,
                Brand         = seed.Brand,
                Name          = seed.Name,
                Concentration = seed.Concentration,
                ImageUrl      = imageUrl,
            });
        }

        await db.SaveChangesAsync();

        // 4. Backfill images for any rows that are missing them
        await BackfillImagesAsync(db, storage);
    }

    /// <summary>
    /// Generates and uploads an SVG placeholder, stores the public URL on the perfume row.
    /// Safe to call when perfume already has an imageUrl — just skips those rows.
    /// </summary>
    private static async Task BackfillImagesAsync(AppDbContext db, StorageService storage)
    {
        var missing = await db.Perfumes.Where(p => p.ImageUrl == null).ToListAsync();
        if (missing.Count == 0) return;

        foreach (var perfume in missing)
        {
            var seed = SeedData.FirstOrDefault(s => s.Id == perfume.Id.ToString());
            var accent = string.IsNullOrEmpty(seed.Accent) ? "#b48c3c" : seed.Accent;
            perfume.ImageUrl = await UploadPlaceholderAsync(storage, perfume.Brand, perfume.Name, accent);
        }

        await db.SaveChangesAsync();
    }

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
        s.ToLowerInvariant().Replace(" ", "-").Replace("'", "").Replace(".", "").Replace("&", "and");

    private static string Escape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}