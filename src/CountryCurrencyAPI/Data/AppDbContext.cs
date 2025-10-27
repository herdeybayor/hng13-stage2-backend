using Microsoft.EntityFrameworkCore;
using CountryCurrencyAPI.Models;

namespace CountryCurrencyAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Country> Countries { get; set; }
    public DbSet<SystemMetadata> SystemMetadata { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Country configuration
        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint on Name (case-insensitive)
            entity.HasIndex(e => e.Name).IsUnique();

            // Indexes for filtering
            entity.HasIndex(e => e.Region);
            entity.HasIndex(e => e.CurrencyCode);

            // Precision for decimal fields
            entity.Property(e => e.ExchangeRate)
                .HasPrecision(18, 6);

            entity.Property(e => e.EstimatedGdp)
                .HasPrecision(20, 2);
        });

        // SystemMetadata configuration
        modelBuilder.Entity<SystemMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.KeyName).IsUnique();
        });
    }
}