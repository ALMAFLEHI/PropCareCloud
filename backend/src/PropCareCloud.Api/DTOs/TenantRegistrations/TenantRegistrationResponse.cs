using PropCareCloud.Api.Domain.Enums;

namespace PropCareCloud.Api.DTOs.TenantRegistrations;

public sealed record TenantRegistrationResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? RequestedPropertyOrUnit,
    string? Note,
    TenantRegistrationStatus Status,
    string StatusDisplayName,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    Guid? ReviewedByUserProfileId,
    string? ReviewedByName,
    string? ReviewNote,
    Guid? ApprovedUserProfileId,
    Guid? ApprovedRentalUnitId,
    string? ApprovedPropertyName,
    string? ApprovedUnitNumber);
