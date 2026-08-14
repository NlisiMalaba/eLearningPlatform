namespace EduZim.Application.Marketplace.DTOs;

public sealed record ContentPackDetailDto(
    ContentPackDto Pack,
    IReadOnlyList<Guid> ContentItemIds);
