using EduZim.Domain.Enums;

namespace EduZim.Application.Marketplace.DTOs;

public sealed record ContentPackDto(
    Guid Id,
    Guid OriginatingTenantId,
    string Title,
    string Description,
    ContentPackStatus Status,
    string SchoolName,
    string TeacherName,
    double AverageRating,
    int RatingCount,
    bool HasAccess);
