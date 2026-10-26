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
            entity.HasOne(changa => changa.Assignment)
                .WithOne(assignment => assignment.Changa)
                .HasForeignKey<ChangaAssignment>(assignment => assignment.ChangaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(changa => changa.CancellationRequests)
                .WithOne(request => request.Changa)
                .HasForeignKey(request => request.ChangaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ChangaAssignment>(entity =>
        {
            entity.HasKey(assignment => assignment.Id);
            entity.Property(assignment => assignment.WorkerId).HasMaxLength(450).IsRequired();
            entity.Property(assignment => assignment.RequesterPhoneConsent).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(assignment => assignment.RequesterPhone).HasMaxLength(ChangaPolicy.MaximumPhoneLength);
            entity.Property(assignment => assignment.WorkerPhoneConsent).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(assignment => assignment.WorkerPhone).HasMaxLength(ChangaPolicy.MaximumPhoneLength);
            entity.HasIndex(assignment => assignment.ChangaId).IsUnique();
        });
        builder.Entity<ChangaCancellationRequest>(entity =>
        {
            entity.HasKey(request => request.Id);
            entity.Property(request => request.RequestedById).HasMaxLength(450).IsRequired();
            entity.Property(request => request.TargetId).HasMaxLength(450).IsRequired();
            entity.Property(request => request.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasIndex(request => request.ChangaId)
                .IsUnique()
                .HasFilter("[Status] = 'Pending'");
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
