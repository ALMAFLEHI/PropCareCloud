using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PropCareCloud.Api.Configuration;
using PropCareCloud.Api.Controllers;
using PropCareCloud.Api.Data;
using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;
using PropCareCloud.Api.DTOs.MaintenanceAttachments;
using PropCareCloud.Api.Services;

namespace PropCareCloud.Api.Tests;

public sealed class MaintenanceAttachmentServiceTests
{
    [Fact]
    public void ControllerRequiresAuthenticationForEveryPortalRole()
    {
        var attribute = typeof(MaintenanceAttachmentsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal("AllRoles", attribute.Policy);
    }

    [Theory]
    [InlineData(UserRole.AdminOwner)]
    [InlineData(UserRole.PropertyManager)]
    [InlineData(UserRole.Tenant)]
    [InlineData(UserRole.MaintenanceStaff)]
    public async Task AuthorizedRequestScopeCanCreateUploadAuthorization(UserRole role)
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        var profileId = role switch
        {
            UserRole.AdminOwner => seed.Admin.Id,
            UserRole.PropertyManager => seed.Manager.Id,
            UserRole.Tenant => seed.Tenant.Id,
            UserRole.MaintenanceStaff => seed.AssignedStaff.Id,
            _ => throw new ArgumentOutOfRangeException(nameof(role))
        };
        var gateway = new FakeAttachmentGateway();
        var service = CreateService(dbContext, profileId, role, gateway);

        var result = await service.CreateUploadAuthorizationAsync(
            seed.Request.Id,
            ValidUploadRequest(),
            CancellationToken.None);

        Assert.Equal(AttachmentServiceStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(1, gateway.UploadCalls);
    }

    [Fact]
    public async Task TenantCannotAuthorizeUploadForAnotherTenantsRequest()
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        var gateway = new FakeAttachmentGateway();
        var service = CreateService(
            dbContext,
            seed.OtherTenant.Id,
            UserRole.Tenant,
            gateway);

        var result = await service.CreateUploadAuthorizationAsync(
            seed.Request.Id,
            ValidUploadRequest(),
            CancellationToken.None);

        Assert.Equal(AttachmentServiceStatus.Forbidden, result.Status);
        Assert.Equal(0, gateway.UploadCalls);
    }

    [Fact]
    public async Task UnassignedStaffCannotAuthorizeUpload()
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        var gateway = new FakeAttachmentGateway();
        var service = CreateService(
            dbContext,
            seed.UnassignedStaff.Id,
            UserRole.MaintenanceStaff,
            gateway);

        var result = await service.CreateUploadAuthorizationAsync(
            seed.Request.Id,
            ValidUploadRequest(),
            CancellationToken.None);

        Assert.Equal(AttachmentServiceStatus.Forbidden, result.Status);
        Assert.Equal(0, gateway.UploadCalls);
    }

    [Theory]
    [InlineData("application/octet-stream", 1024)]
    [InlineData("image/png", 10485761)]
    [InlineData("image/png", 0)]
    public async Task InvalidUploadMetadataIsRejected(string contentType, long sizeBytes)
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        var gateway = new FakeAttachmentGateway();
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant, gateway);

        var result = await service.CreateUploadAuthorizationAsync(
            seed.Request.Id,
            new AttachmentUploadRequest
            {
                FileName = "evidence.png",
                ContentType = contentType,
                SizeBytes = sizeBytes
            },
            CancellationToken.None);

        Assert.Equal(AttachmentServiceStatus.ValidationFailed, result.Status);
        Assert.Equal(0, gateway.UploadCalls);
    }

    [Fact]
    public void UnsafePathElementsAreRemovedFromFileName()
    {
        var valid = AttachmentFilePolicy.TryValidate(
            "../../tenant evidence.pdf",
            "application/pdf",
            500,
            Task2AttachmentOptions.DefaultMaxFileSizeBytes,
            out var safeFileName,
            out _);

        Assert.True(valid);
        Assert.Equal("tenant evidence.pdf", safeFileName);
    }

    [Fact]
    public async Task ConfirmUploadVerifiesObjectAndStoresMetadata()
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        var key = ValidObjectKey(seed.Request.Id);
        var gateway = new FakeAttachmentGateway();
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant, gateway);

        var result = await service.ConfirmUploadAsync(
            seed.Request.Id,
            ValidConfirmRequest(key),
            CancellationToken.None);

        Assert.Equal(AttachmentServiceStatus.Success, result.Status);
        Assert.Equal(1, gateway.VerifyCalls);
        var saved = await dbContext.MaintenanceRequestAttachments.SingleAsync();
        Assert.Equal(key, saved.StorageKey);
        Assert.Equal(1024, saved.SizeBytes);
        Assert.Equal(seed.Tenant.Id, saved.UploadedByUserProfileId);
    }

    [Fact]
    public async Task ConfirmUploadPublishesAttachmentEventAfterMetadataPersistence()
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        var publisher = new RecordingNotificationPublisher();
        var service = CreateService(
            dbContext,
            seed.Tenant.Id,
            UserRole.Tenant,
            new FakeAttachmentGateway(),
            publisher);

        var result = await service.ConfirmUploadAsync(
            seed.Request.Id,
            ValidConfirmRequest(ValidObjectKey(seed.Request.Id)),
            CancellationToken.None);

        Assert.Equal(AttachmentServiceStatus.Success, result.Status);
        Assert.True(result.Value!.NotificationQueued);
        var published = Assert.Single(publisher.PublishedEvents);
        Assert.Equal("AttachmentConfirmed", published.EventType);
        Assert.Equal(seed.Request.Id, published.MaintenanceRequestId);
        Assert.Equal(
            [seed.Tenant.Id, seed.AssignedStaff.Id],
            published.TargetProfileIds);
        Assert.Single(dbContext.MaintenanceRequestAttachments);
    }

    [Fact]
    public async Task FailedObjectVerificationDoesNotStoreMetadata()
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        var gateway = new FakeAttachmentGateway { VerificationSucceeds = false };
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant, gateway);

        var result = await service.ConfirmUploadAsync(
            seed.Request.Id,
            ValidConfirmRequest(ValidObjectKey(seed.Request.Id)),
            CancellationToken.None);

        Assert.Equal(AttachmentServiceStatus.ValidationFailed, result.Status);
        Assert.Empty(dbContext.MaintenanceRequestAttachments);
    }

    [Fact]
    public async Task DuplicateObjectKeyCannotBeConfirmedTwice()
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        var key = ValidObjectKey(seed.Request.Id);
        dbContext.MaintenanceRequestAttachments.Add(new MaintenanceRequestAttachment
        {
            MaintenanceRequestId = seed.Request.Id,
            UploadedByUserProfileId = seed.Tenant.Id,
            FileName = "evidence.png",
            ContentType = "image/png",
            SizeBytes = 1024,
            StorageKey = key
        });
        await dbContext.SaveChangesAsync();
        var gateway = new FakeAttachmentGateway();
        var service = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant, gateway);

        var result = await service.ConfirmUploadAsync(
            seed.Request.Id,
            ValidConfirmRequest(key),
            CancellationToken.None);

        Assert.Equal(AttachmentServiceStatus.Conflict, result.Status);
        Assert.Equal(0, gateway.VerifyCalls);
        Assert.Single(dbContext.MaintenanceRequestAttachments);
    }

    [Fact]
    public async Task AttachmentListingPreservesTenantIsolation()
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        dbContext.MaintenanceRequestAttachments.Add(new MaintenanceRequestAttachment
        {
            MaintenanceRequestId = seed.Request.Id,
            UploadedByUserProfileId = seed.Tenant.Id,
            FileName = "evidence.png",
            ContentType = "image/png",
            SizeBytes = 1024,
            StorageKey = ValidObjectKey(seed.Request.Id)
        });
        await dbContext.SaveChangesAsync();
        var gateway = new FakeAttachmentGateway();
        var ownerService = CreateService(dbContext, seed.Tenant.Id, UserRole.Tenant, gateway);
        var otherService = CreateService(
            dbContext,
            seed.OtherTenant.Id,
            UserRole.Tenant,
            gateway);

        var ownerResult = await ownerService.GetAttachmentsAsync(
            seed.Request.Id,
            CancellationToken.None);
        var otherResult = await otherService.GetAttachmentsAsync(
            seed.Request.Id,
            CancellationToken.None);

        Assert.Single(ownerResult.Value!);
        Assert.Equal(AttachmentServiceStatus.Forbidden, otherResult.Status);
        Assert.Null(otherResult.Value);
    }

    [Fact]
    public async Task AuthorizedDownloadUsesStoredKeyOnly()
    {
        await using var dbContext = CreateContext();
        var seed = await SeedAsync(dbContext);
        var attachment = new MaintenanceRequestAttachment
        {
            MaintenanceRequestId = seed.Request.Id,
            UploadedByUserProfileId = seed.Tenant.Id,
            FileName = "evidence.png",
            ContentType = "image/png",
            SizeBytes = 1024,
            StorageKey = ValidObjectKey(seed.Request.Id)
        };
        dbContext.MaintenanceRequestAttachments.Add(attachment);
        await dbContext.SaveChangesAsync();
        var gateway = new FakeAttachmentGateway();
        var service = CreateService(dbContext, seed.AssignedStaff.Id, UserRole.MaintenanceStaff, gateway);

        var result = await service.CreateDownloadAuthorizationAsync(
            seed.Request.Id,
            attachment.Id,
            CancellationToken.None);

        Assert.Equal(AttachmentServiceStatus.Success, result.Status);
        Assert.Equal(1, gateway.DownloadCalls);
        Assert.Equal(attachment.StorageKey, gateway.LastDownloadRequest?.ObjectKey);
    }

    private static AttachmentUploadRequest ValidUploadRequest() => new()
    {
        FileName = "evidence.png",
        ContentType = "image/png",
        SizeBytes = 1024
    };

    private static AttachmentConfirmRequest ValidConfirmRequest(string objectKey) => new()
    {
        ObjectKey = objectKey,
        FileName = "evidence.png",
        ContentType = "image/png",
        SizeBytes = 1024
    };

    private static string ValidObjectKey(Guid requestId) =>
        $"maintenance-requests/{requestId:D}/{Guid.NewGuid():D}-evidence.png";

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"propcare-attachments-{Guid.NewGuid()}")
            .Options);

    private static MaintenanceAttachmentService CreateService(
        AppDbContext dbContext,
        Guid userProfileId,
        UserRole role,
        FakeAttachmentGateway gateway,
        IUserNotificationService? notificationPublisher = null)
    {
        var currentUser = new FakeCurrentUserService(userProfileId, role);
        var publisher = notificationPublisher ?? new RecordingNotificationPublisher();
        var requestService = new MaintenanceRequestService(dbContext, currentUser, publisher);
        return new MaintenanceAttachmentService(
            dbContext,
            requestService,
            currentUser,
            gateway,
            Options.Create(new Task2AttachmentOptions
            {
                ApiBaseUrl = "https://attachment-api.example.test/prod",
                ApiKey = "unit-test-placeholder",
                MaxFileSizeBytes = Task2AttachmentOptions.DefaultMaxFileSizeBytes,
                UrlExpirySeconds = Task2AttachmentOptions.DefaultUrlExpirySeconds
            }),
            publisher);
    }

    private static async Task<AttachmentSeed> SeedAsync(AppDbContext dbContext)
    {
        var property = new Property
        {
            Name = "Attachment Residence",
            AddressLine1 = "1 Secure Upload Way",
            City = "Kuala Lumpur",
            Country = "Malaysia",
            Status = PropertyStatus.Active
        };
        var unit = new RentalUnit
        {
            PropertyId = property.Id,
            Property = property,
            UnitNumber = "S-1701",
            Status = UnitStatus.Occupied
        };
        var tenant = User("Attachment Tenant", "tenant.attach@example.com", UserRole.Tenant);
        var otherTenant = User("Other Tenant", "tenant.other@example.com", UserRole.Tenant);
        var admin = User("Attachment Admin", "admin.attach@example.com", UserRole.AdminOwner);
        var manager = User("Attachment Manager", "manager.attach@example.com", UserRole.PropertyManager);
        var assignedStaff = User("Assigned Staff", "staff.assigned@example.com", UserRole.MaintenanceStaff);
        var unassignedStaff = User("Other Staff", "staff.other@example.com", UserRole.MaintenanceStaff);
        var request = new MaintenanceRequest
        {
            RentalUnitId = unit.Id,
            RentalUnit = unit,
            TenantProfileId = tenant.Id,
            TenantProfile = tenant,
            AssignedStaffProfileId = assignedStaff.Id,
            AssignedStaffProfile = assignedStaff,
            Title = "Secure attachment test",
            Description = "Request used to test role-scoped attachment access.",
            Category = MaintenanceCategory.Other,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceStatus.Assigned
        };

        dbContext.Properties.Add(property);
        dbContext.RentalUnits.Add(unit);
        dbContext.UserProfiles.AddRange(
            tenant,
            otherTenant,
            admin,
            manager,
            assignedStaff,
            unassignedStaff);
        dbContext.MaintenanceRequests.Add(request);
        await dbContext.SaveChangesAsync();
        return new AttachmentSeed(
            tenant,
            otherTenant,
            admin,
            manager,
            assignedStaff,
            unassignedStaff,
            request);
    }

    private static UserProfile User(string name, string email, UserRole role) => new()
    {
        FullName = name,
        Email = email,
        Role = role,
        IsActive = true
    };

    private sealed record AttachmentSeed(
        UserProfile Tenant,
        UserProfile OtherTenant,
        UserProfile Admin,
        UserProfile Manager,
        UserProfile AssignedStaff,
        UserProfile UnassignedStaff,
        MaintenanceRequest Request);

    private sealed class FakeAttachmentGateway : ITask2AttachmentGateway
    {
        public bool IsConfigured => true;
        public bool VerificationSucceeds { get; init; } = true;
        public int UploadCalls { get; private set; }
        public int VerifyCalls { get; private set; }
        public int DownloadCalls { get; private set; }
        public GatewayDownloadRequest? LastDownloadRequest { get; private set; }

        public Task<AttachmentGatewayResult<GatewayUploadAuthorization>>
            CreateUploadAuthorizationAsync(
                GatewayUploadRequest request,
                CancellationToken cancellationToken)
        {
            UploadCalls++;
            return Task.FromResult(AttachmentGatewayResult<GatewayUploadAuthorization>.Success(
                new GatewayUploadAuthorization(
                    "https://private-upload.example.test",
                    new Dictionary<string, string> { ["key"] = "signed" },
                    ValidObjectKey(request.RequestId),
                    300)));
        }

        public Task<AttachmentGatewayResult<GatewayVerificationResponse>> VerifyUploadAsync(
            GatewayVerificationRequest request,
            CancellationToken cancellationToken)
        {
            VerifyCalls++;
            return Task.FromResult(AttachmentGatewayResult<GatewayVerificationResponse>.Success(
                new GatewayVerificationResponse(
                    VerificationSucceeds,
                    request.ObjectKey,
                    request.ContentType,
                    request.SizeBytes)));
        }

        public Task<AttachmentGatewayResult<GatewayDownloadAuthorization>>
            CreateDownloadAuthorizationAsync(
                GatewayDownloadRequest request,
                CancellationToken cancellationToken)
        {
            DownloadCalls++;
            LastDownloadRequest = request;
            return Task.FromResult(AttachmentGatewayResult<GatewayDownloadAuthorization>.Success(
                new GatewayDownloadAuthorization(
                    "https://private-download.example.test/signed",
                    300)));
        }
    }

    private sealed class FakeCurrentUserService(Guid userProfileId, UserRole role)
        : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserProfileId => userProfileId;
        public string? Email => "attachment-test@propcare.local";
        public UserRole? Role => role;
        public bool IsAdminOwner => role == UserRole.AdminOwner;
        public bool IsPropertyManager => role == UserRole.PropertyManager;
        public bool IsTenant => role == UserRole.Tenant;
        public bool IsMaintenanceStaff => role == UserRole.MaintenanceStaff;
        public bool IsAdminOrManager => role is UserRole.AdminOwner or UserRole.PropertyManager;
        public bool HasRole(params UserRole[] roles) => roles.Contains(role);
    }
}
