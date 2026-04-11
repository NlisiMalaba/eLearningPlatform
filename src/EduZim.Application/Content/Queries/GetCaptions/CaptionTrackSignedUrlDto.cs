namespace EduZim.Application.Content.Queries.GetCaptions;

public sealed record CaptionTrackSignedUrlDto(Guid TrackId, string Language, string SignedUrl);
