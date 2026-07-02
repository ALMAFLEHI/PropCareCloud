using Microsoft.EntityFrameworkCore;
using PropCareCloud.Api.Domain.Entities;

namespace PropCareCloud.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<RentalUnit> RentalUnits => Set<RentalUnit>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<MaintenanceRequestComment> MaintenanceRequestComments => Set<MaintenanceRequestComment>();
    public DbSet<MaintenanceRequestAttachment> MaintenanceRequestAttachments => Set<MaintenanceRequestAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserProfile(modelBuilder);
        ConfigureProperty(modelBuilder);
        ConfigureRentalUnit(modelBuilder);
        ConfigureMaintenanceRequest(modelBuilder);
        ConfigureMaintenanceRequestComment(modelBuilder);
        ConfigureMaintenanceRequestAttachment(modelBuilder);
    }

    private static void ConfigureUserProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles");
            entity.HasKey(user => user.Id);

            entity.Property(user => user.IdentityUserId).HasMaxLength(150);
            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(256).IsRequired();
            entity.Property(user => user.PhoneNumber).HasMaxLength(30);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(user => user.IsActive).IsRequired();
            entity.Property(user => user.CreatedAtUtc).IsRequired();

            entity.HasIndex(user => user.Email);
            entity.HasIndex(user => user.IdentityUserId);
        });
    }

    private static void ConfigureProperty(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Property>(entity =>
        {
            entity.ToTable("properties");
            entity.HasKey(property => property.Id);

            entity.Property(property => property.Name).HasMaxLength(150).IsRequired();
            entity.Property(property => property.AddressLine1).HasMaxLength(250).IsRequired();
            entity.Property(property => property.AddressLine2).HasMaxLength(250);
            entity.Property(property => property.City).HasMaxLength(100).IsRequired();
            entity.Property(property => property.Country).HasMaxLength(100).IsRequired();
            entity.Property(property => property.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(property => property.CreatedAtUtc).IsRequired();

            entity.HasMany(property => property.Units)
                .WithOne(unit => unit.Property)
                .HasForeignKey(unit => unit.PropertyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRentalUnit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RentalUnit>(entity =>
        {
            entity.ToTable("rental_units");
            entity.HasKey(unit => unit.Id);

            entity.Property(unit => unit.PropertyId).IsRequired();
            entity.Property(unit => unit.UnitNumber).HasMaxLength(50).IsRequired();
            entity.Property(unit => unit.Floor).HasMaxLength(50);
            entity.Property(unit => unit.Bedrooms);
            entity.Property(unit => unit.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(unit => unit.CreatedAtUtc).IsRequired();

            entity.HasMany(unit => unit.MaintenanceRequests)
                .WithOne(request => request.RentalUnit)
                .HasForeignKey(request => request.RentalUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureMaintenanceRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceRequest>(entity =>
        {
            entity.ToTable("maintenance_requests");
            entity.HasKey(request => request.Id);

            entity.Property(request => request.RentalUnitId).IsRequired();
            entity.Property(request => request.TenantProfileId).IsRequired();
            entity.Property(request => request.AssignedStaffProfileId);
            entity.Property(request => request.Title).HasMaxLength(200).IsRequired();
            entity.Property(request => request.Description).HasMaxLength(2000).IsRequired();
            entity.Property(request => request.Category).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(request => request.Priority).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(request => request.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            entity.Property(request => request.CreatedAtUtc).IsRequired();
            entity.Property(request => request.UpdatedAtUtc);
            entity.Property(request => request.CompletedAtUtc);

            entity.HasOne(request => request.TenantProfile)
                .WithMany(user => user.TenantRequests)
                .HasForeignKey(request => request.TenantProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.AssignedStaffProfile)
                .WithMany(user => user.AssignedMaintenanceRequests)
                .HasForeignKey(request => request.AssignedStaffProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(request => request.Comments)
                .WithOne(comment => comment.MaintenanceRequest)
                .HasForeignKey(comment => comment.MaintenanceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(request => request.Attachments)
                .WithOne(attachment => attachment.MaintenanceRequest)
                .HasForeignKey(attachment => attachment.MaintenanceRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureMaintenanceRequestComment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceRequestComment>(entity =>
        {
            entity.ToTable("maintenance_request_comments");
            entity.HasKey(comment => comment.Id);

            entity.Property(comment => comment.MaintenanceRequestId).IsRequired();
            entity.Property(comment => comment.UserProfileId).IsRequired();
            entity.Property(comment => comment.CommentText).HasMaxLength(2000).IsRequired();
            entity.Property(comment => comment.IsInternal).IsRequired();
            entity.Property(comment => comment.CreatedAtUtc).IsRequired();

            entity.HasOne(comment => comment.UserProfile)
                .WithMany(user => user.Comments)
                .HasForeignKey(comment => comment.UserProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureMaintenanceRequestAttachment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MaintenanceRequestAttachment>(entity =>
        {
            entity.ToTable("maintenance_request_attachments");
            entity.HasKey(attachment => attachment.Id);

            entity.Property(attachment => attachment.MaintenanceRequestId).IsRequired();
            entity.Property(attachment => attachment.UploadedByUserProfileId).IsRequired();
            entity.Property(attachment => attachment.FileName).HasMaxLength(255).IsRequired();
            entity.Property(attachment => attachment.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(attachment => attachment.StorageKey).HasMaxLength(500).IsRequired();
            entity.Property(attachment => attachment.UploadedAtUtc).IsRequired();

            entity.HasOne(attachment => attachment.UploadedByUserProfile)
                .WithMany(user => user.UploadedAttachments)
                .HasForeignKey(attachment => attachment.UploadedByUserProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
