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
    public DbSet<AuthUserAccount> AuthUserAccounts => Set<AuthUserAccount>();
    public DbSet<TenantUnitAssignment> TenantUnitAssignments => Set<TenantUnitAssignment>();
    public DbSet<TenantRegistrationRequest> TenantRegistrationRequests => Set<TenantRegistrationRequest>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserProfile(modelBuilder);
        ConfigureProperty(modelBuilder);
        ConfigureRentalUnit(modelBuilder);
        ConfigureMaintenanceRequest(modelBuilder);
        ConfigureMaintenanceRequestComment(modelBuilder);
        ConfigureMaintenanceRequestAttachment(modelBuilder);
        ConfigureAuthUserAccount(modelBuilder);
        ConfigureTenantUnitAssignment(modelBuilder);
        ConfigureTenantRegistrationRequest(modelBuilder);
        ConfigureUserNotification(modelBuilder);
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
            entity.Property(attachment => attachment.SizeBytes).IsRequired();
            entity.Property(attachment => attachment.StorageKey).HasMaxLength(500).IsRequired();
            entity.Property(attachment => attachment.UploadedAtUtc).IsRequired();

            entity.HasIndex(attachment => attachment.MaintenanceRequestId);
            entity.HasIndex(attachment => attachment.StorageKey).IsUnique();

            entity.HasOne(attachment => attachment.UploadedByUserProfile)
                .WithMany(user => user.UploadedAttachments)
                .HasForeignKey(attachment => attachment.UploadedByUserProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuthUserAccount(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuthUserAccount>(entity =>
        {
            entity.ToTable("auth_user_accounts");
            entity.HasKey(account => account.Id);

            entity.Property(account => account.UserProfileId).IsRequired();
            entity.Property(account => account.Email).HasMaxLength(256).IsRequired();
            entity.Property(account => account.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(account => account.IsActive).IsRequired();
            entity.Property(account => account.CreatedAtUtc).IsRequired();
            entity.Property(account => account.LastLoginAtUtc);

            entity.HasIndex(account => account.Email).IsUnique();
            entity.HasIndex(account => account.UserProfileId).IsUnique();

            entity.HasOne(account => account.UserProfile)
                .WithOne(user => user.AuthUserAccount)
                .HasForeignKey<AuthUserAccount>(account => account.UserProfileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTenantUnitAssignment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantUnitAssignment>(entity =>
        {
            entity.ToTable("tenant_unit_assignments");
            entity.HasKey(assignment => assignment.Id);

            entity.Property(assignment => assignment.TenantProfileId).IsRequired();
            entity.Property(assignment => assignment.RentalUnitId).IsRequired();
            entity.Property(assignment => assignment.LeaseStartDateUtc).IsRequired();
            entity.Property(assignment => assignment.LeaseEndDateUtc);
            entity.Property(assignment => assignment.IsActive).IsRequired();
            entity.Property(assignment => assignment.CreatedAtUtc).IsRequired();

            entity.HasIndex(assignment => assignment.TenantProfileId);
            entity.HasIndex(assignment => assignment.RentalUnitId);
            entity.HasIndex(assignment => assignment.RentalUnitId)
                .IsUnique()
                .HasFilter("\"IsActive\" = TRUE AND \"LeaseEndDateUtc\" IS NULL");

            entity.HasOne(assignment => assignment.TenantProfile)
                .WithMany(user => user.TenantUnitAssignments)
                .HasForeignKey(assignment => assignment.TenantProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(assignment => assignment.RentalUnit)
                .WithMany(unit => unit.TenantAssignments)
                .HasForeignKey(assignment => assignment.RentalUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTenantRegistrationRequest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantRegistrationRequest>(entity =>
        {
            entity.ToTable("tenant_registration_requests");
            entity.HasKey(request => request.Id);

            entity.Property(request => request.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(request => request.LastName).HasMaxLength(100).IsRequired();
            entity.Property(request => request.Email).HasMaxLength(256).IsRequired();
            entity.Property(request => request.PhoneNumber).HasMaxLength(30);
            entity.Property(request => request.RequestedPropertyOrUnit).HasMaxLength(250);
            entity.Property(request => request.Note).HasMaxLength(1000);
            entity.Property(request => request.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(request => request.SubmittedAtUtc).IsRequired();
            entity.Property(request => request.ReviewedAtUtc);
            entity.Property(request => request.ReviewedByUserProfileId);
            entity.Property(request => request.ReviewNote).HasMaxLength(1000);
            entity.Property(request => request.ApprovedUserProfileId);
            entity.Property(request => request.ApprovedRentalUnitId);

            entity.HasIndex(request => request.Email);
            entity.HasIndex(request => request.Status);
            entity.HasIndex(request => new { request.Email, request.Status });

            entity.HasOne(request => request.ReviewedByUserProfile)
                .WithMany()
                .HasForeignKey(request => request.ReviewedByUserProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.ApprovedUserProfile)
                .WithMany()
                .HasForeignKey(request => request.ApprovedUserProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(request => request.ApprovedRentalUnit)
                .WithMany()
                .HasForeignKey(request => request.ApprovedRentalUnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureUserNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("user_notifications");
            entity.HasKey(notification => notification.Id);

            entity.Property(notification => notification.UserProfileId).IsRequired();
            entity.Property(notification => notification.EventId).IsRequired();
            entity.Property(notification => notification.CorrelationId).IsRequired();
            entity.Property(notification => notification.EventType).HasMaxLength(100).IsRequired();
            entity.Property(notification => notification.Title).HasMaxLength(120).IsRequired();
            entity.Property(notification => notification.Message).HasMaxLength(500).IsRequired();
            entity.Property(notification => notification.IsRead).IsRequired();
            entity.Property(notification => notification.CreatedAtUtc).IsRequired();

            entity.HasIndex(notification => new
                {
                    notification.UserProfileId,
                    notification.EventId
                })
                .IsUnique();
            entity.HasIndex(notification => new
                {
                    notification.UserProfileId,
                    notification.IsRead,
                    notification.CreatedAtUtc
                });

            entity.HasOne(notification => notification.UserProfile)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(notification => notification.MaintenanceRequest)
                .WithMany(request => request.Notifications)
                .HasForeignKey(notification => notification.MaintenanceRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
