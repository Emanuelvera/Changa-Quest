using ChangaQuest.Entities.Accounts;
using ChangaQuest.Entities.Changas;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChangaQuest.DataAccess.Accounts;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Changa> Changas => Set<Changa>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Changa>(entity =>
        {
            entity.HasKey(changa => changa.Id);
            entity.Property(changa => changa.Description).HasMaxLength(ChangaPolicy.MaximumDescriptionLength).IsRequired();
            entity.Property(changa => changa.Zone).HasMaxLength(ChangaPolicy.MaximumZoneLength).IsRequired();
            entity.Property(changa => changa.ExactAddress).HasMaxLength(ChangaPolicy.MaximumExactAddressLength).IsRequired();
            entity.Property(changa => changa.Latitude).HasPrecision(9, 6);
            entity.Property(changa => changa.Longitude).HasPrecision(9, 6);
            entity.Property(changa => changa.MaterialsProvider).HasConversion<string>().HasMaxLength(16);
            entity.Property(changa => changa.AssignmentMode).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(changa => changa.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(changa => changa.IsSample);
        });
        builder.Entity<AppUser>()
            .Property(user => user.SearchLatitude)
            .HasPrecision(9, 6);
        builder.Entity<AppUser>()
            .Property(user => user.SearchLongitude)
            .HasPrecision(9, 6);
        builder.Entity<AppUser>()
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("EmailIndex");
    }
}
