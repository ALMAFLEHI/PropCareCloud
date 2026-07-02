using PropCareCloud.Api.Domain.Entities;
using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.Tests;

public sealed class DomainEntityTests
{
    [Fact]
    public void Property_CanHaveUnitsCollection()
    {
        var property = new Property
        {
            Name = "Harbor Residence",
            AddressLine1 = "22 Marina Road",
            City = "Kuala Lumpur",
            Country = "Malaysia",
            Status = PropertyStatus.Active
        };

        property.Units.Add(new RentalUnit
        {
            PropertyId = property.Id,
            UnitNumber = "A-1201",
            Status = UnitStatus.Occupied
        });

        Assert.Single(property.Units);
        Assert.Equal("Harbor Residence", property.Name);
    }

    [Fact]
    public void MaintenanceRequest_CanHaveCommentsAndAttachmentsCollections()
    {
        var request = new MaintenanceRequest
        {
            RentalUnitId = Guid.NewGuid(),
            TenantProfileId = Guid.NewGuid(),
            Title = "Kitchen sink leak",
            Description = "Water is leaking under the kitchen sink.",
            Category = MaintenanceCategory.Plumbing,
            Priority = MaintenancePriority.High,
            Status = MaintenanceStatus.Submitted
        };

        request.Comments.Add(new MaintenanceRequestComment
        {
            MaintenanceRequestId = request.Id,
            UserProfileId = request.TenantProfileId,
            CommentText = "Leak is still active."
        });
        request.Attachments.Add(new MaintenanceRequestAttachment
        {
            MaintenanceRequestId = request.Id,
            UploadedByUserProfileId = request.TenantProfileId,
            FileName = "sink-leak.jpg",
            ContentType = "image/jpeg",
            StorageKey = "future-s3/requests/sink-leak.jpg"
        });

        Assert.Single(request.Comments);
        Assert.Single(request.Attachments);
        Assert.Equal("Kitchen sink leak", request.Title);
        Assert.False(string.IsNullOrWhiteSpace(request.Description));
    }
}
