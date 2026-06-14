using Microsoft.EntityFrameworkCore;
using ScentMarket.Shared;

namespace ScentMarekt.Server.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Perfume> Perfumes => Set<Perfume>();

    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<OfferPrice> OfferPrices => Set<OfferPrice>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(200).IsRequired(false);
            entity.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");

            entity.HasMany(x => x.SoldOffers)
                .WithOne(x => x.Seller)
                .HasForeignKey(x => x.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.BoughtTransactions)
                .WithOne(x => x.Buyer)
                .HasForeignKey(x => x.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.WrittenReviews)
                .WithOne(x => x.Reviewer)
                .HasForeignKey(x => x.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.ReceivedReviews)
                .WithOne(x => x.Reviewee)
                .HasForeignKey(x => x.RevieweeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Perfume>(entity =>
        {
            entity.ToTable("Perfumes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Brand).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Concentration).HasMaxLength(100).IsRequired();

            entity.HasMany(x => x.Offers)
                .WithOne(x => x.Perfume)
                .HasForeignKey(x => x.PerfumeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.ToTable("Offers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AvailableVolumeMl).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            entity.HasMany(x => x.Prices)
                .WithOne(x => x.Offer)
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Transactions)
                .WithOne(x => x.Offer)
                .HasForeignKey(x => x.OfferId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OfferPrice>(entity =>
        {
            entity.ToTable("OfferPrices");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CapacityMl).IsRequired();
            entity.Property(x => x.Price).HasPrecision(18, 2).IsRequired();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.VolumeBoughtMl).IsRequired();
            entity.Property(x => x.TotalPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            entity.HasMany(x => x.Reviews)
                .WithOne(x => x.Transaction)
                .HasForeignKey(x => x.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("Reviews");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Rating).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            entity.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5");
        });
    }
}
