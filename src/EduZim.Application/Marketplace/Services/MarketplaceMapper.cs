using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Application.Marketplace.DTOs;

namespace EduZim.Application.Marketplace.Services;

public static class MarketplaceMapper
{
    public static bool HasCompleteAttribution(ContentPack pack) =>
        !string.IsNullOrWhiteSpace(pack.SchoolName) && !string.IsNullOrWhiteSpace(pack.TeacherName);

    public static ContentPackDto ToDto(
        ContentPack pack,
        IReadOnlyList<ContentPackRating> ratings,
        bool hasAccess)
    {
        (double average, int count) = Average(ratings);
        return new ContentPackDto(
            pack.Id,
            pack.TenantId,
            pack.Title,
            pack.Description,
            pack.Status,
            pack.SchoolName,
            pack.TeacherName,
            average,
            count,
            hasAccess);
    }

    public static ContentPackDetailDto ToDetailDto(
        ContentPack pack,
        IReadOnlyList<ContentPackRating> ratings,
        IReadOnlyList<Guid> contentItemIds,
        bool hasAccess) =>
        new(ToDto(pack, ratings, hasAccess), contentItemIds);

    public static (double Average, int Count) Average(IReadOnlyList<ContentPackRating> ratings)
    {
        if (ratings.Count == 0)
            return (0, 0);

        double sum = 0;
        foreach (ContentPackRating rating in ratings)
            sum += rating.Rating;

        return (Math.Round(sum / ratings.Count, 2), ratings.Count);
    }

    public static bool HasApprovedAccess(
        Guid? callerTenantId,
        ContentPack pack,
        IReadOnlyList<ContentPackAccessRequest> requests)
    {
        if (callerTenantId is null)
            return false;
        if (callerTenantId.Value == pack.TenantId)
            return true;

        return requests.Any(
            r => r.ContentPackId == pack.Id
                && r.RequestingTenantId == callerTenantId.Value
                && r.Status == ContentPackAccessStatus.Approved);
    }
}
