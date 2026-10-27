using ChangaQuest.Entities.Accounts;
using ChangaQuest.Entities.Changas;
using ChangaQuest.Entities.Reports;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChangaQuest.DataAccess.Accounts;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Changa> Changas => Set<Changa>();
    public DbSet<ChangaCategory> ChangaCategories => Set<ChangaCategory>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ChangaRating> ChangaRatings => Set<ChangaRating>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Changa>(entity =>
        {
            entity.HasKey(changa => changa.Id);
            entity.Property(changa => changa.Description).HasMaxLength(ChangaPolicy.MaximumDescriptionLength).IsRequired();
            entity.Property(changa => changa.CategoryId).HasMaxLength(64).IsRequired(false);
            entity.Property(changa => changa.ReviewReason).HasMaxLength(500);
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
        builder.Entity<ChangaCategory>(entity =>
        {
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Id).HasMaxLength(64);
            entity.Property(category => category.Name).HasMaxLength(120).IsRequired();
            entity.Property(category => category.IsApproved).IsRequired();
            entity.HasData(ChangaCategoryCatalog.Approved);
        });
        builder.Entity<ChangaAssignment>(entity =>
        {
            entity.HasKey(assignment => assignment.Id);
            entity.Property(assignment => assignment.WorkerId).HasMaxLength(450).IsRequired();
            entity.Property(assignment => assignment.RequesterPhoneConsent).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(assignment => assignment.RequesterPhone).HasMaxLength(ChangaPolicy.MaximumPhoneLength);
            entity.Property(assignment => assignment.WorkerPhoneConsent).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(assignment => assignment.WorkerPhone).HasMaxLength(ChangaPolicy.MaximumPhoneLength);
            entity.Property(assignment => assignment.CompletionReviewCause).HasMaxLength(16);
            entity.Property(assignment => assignment.RequesterPaymentDeclaredAt);
            entity.Property(assignment => assignment.WorkerPaymentDeclaredAt);
            entity.HasIndex(assignment => assignment.ChangaId).IsUnique();
        });
        builder.Entity<Report>(entity =>
        {
            entity.HasKey(report => report.Id);
            entity.Property(report => report.ReporterId).HasMaxLength(450).IsRequired();
            entity.Property(report => report.Reason).HasMaxLength(ReportPolicy.MaximumReasonLength).IsRequired();
            entity.Property(report => report.Evidence).HasMaxLength(ReportPolicy.MaximumEvidenceLength);
            entity.Property(report => report.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.HasOne<Changa>()
                .WithMany()
                .HasForeignKey(report => report.ChangaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(report => report.ChangaId);
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
        builder.Entity<ChangaRating>(entity =>
        {
            entity.HasKey(rating => rating.Id);
            entity.Property(rating => rating.RaterId).HasMaxLength(450).IsRequired();
            entity.Property(rating => rating.RateeId).HasMaxLength(450).IsRequired();
            entity.Property(rating => rating.Score).IsRequired();
            entity.Property(rating => rating.CreatedAt).IsRequired();
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_ChangaRatings_Score",
                "[Score] >= 1 AND [Score] <= 5"));
            entity.HasOne(rating => rating.Changa)
                .WithMany()
                .HasForeignKey(rating => rating.ChangaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(rating => new { rating.ChangaId, rating.RaterId })
                .IsUnique()
                .HasDatabaseName("IX_ChangaRatings_ChangaId_RaterId");
            entity.HasIndex(rating => rating.RateeId);
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
