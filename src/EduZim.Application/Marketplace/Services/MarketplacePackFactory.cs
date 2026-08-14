using EduZim.Application.Marketplace.Commands.SubmitContentPack;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Application.Marketplace.Services;

public static class MarketplacePackFactory
{
    public static ContentPack CreatePending(
        SubmitContentPackCommand request,
        Guid submittedByUserId,
        string schoolName,
        string teacherName,
        DateTime utcNow)
    {
        Guid packId = Guid.NewGuid();
        return new ContentPack
        {
            Id = packId,
            TenantId = request.TenantId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Status = ContentPackStatus.PendingReview,
            SubmittedByUserId = submittedByUserId,
            SchoolName = schoolName,
            TeacherName = teacherName,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
    }

    public static ContentPackItem CreateItem(
        Guid packId,
        Guid tenantId,
        Guid contentItemId,
        int sequenceOrder,
        DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ContentPackId = packId,
            ContentItemId = contentItemId,
            SequenceOrder = sequenceOrder,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

    public static ContentPackAccessRequest CreatePendingAccessRequest(
        ContentPack pack,
        Guid requestingTenantId,
        Guid requestedByUserId,
        DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = pack.TenantId,
            ContentPackId = pack.Id,
            RequestingTenantId = requestingTenantId,
            RequestedByUserId = requestedByUserId,
            Status = ContentPackAccessStatus.Pending,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };

    public static ContentPackRating CreateRating(
        ContentPack pack,
        Guid raterTenantId,
        Guid userId,
        int rating,
        string? review,
        DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = raterTenantId,
            ContentPackId = pack.Id,
            UserId = userId,
            Rating = rating,
            Review = string.IsNullOrWhiteSpace(review) ? null : review.Trim(),
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
}
