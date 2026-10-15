using Clt.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Currency> Currencies => Set<Currency>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.Property(entity => entity.Name).IsRequired();
        user.Property(entity => entity.Email).IsRequired().UseCollation("NOCASE");
        user.HasIndex(entity => entity.Email).IsUnique();
        user.Property(entity => entity.IsActive).HasDefaultValue(true);
        user.HasMany(entity => entity.Addresses).WithOne(entity => entity.User)
            .HasForeignKey(entity => entity.UserId).OnDelete(DeleteBehavior.Cascade);

        var address = modelBuilder.Entity<Address>();
        address.Property(entity => entity.Street).IsRequired();
        address.Property(entity => entity.City).IsRequired();
        address.Property(entity => entity.Country).IsRequired();

        var currency = modelBuilder.Entity<Currency>();
        currency.Property(entity => entity.Code).IsRequired().UseCollation("NOCASE");
        currency.HasIndex(entity => entity.Code).IsUnique();
        currency.Property(entity => entity.Name).IsRequired();
        currency.Property(entity => entity.RateToBase).IsRequired();
        currency.HasData(
            new Currency { Id = 1, Code = "PYG", Name = "Guaraní paraguayo", RateToBase = 1m },
            new Currency { Id = 2, Code = "USD", Name = "Dólar estadounidense", RateToBase = 7300m });
    }
}
