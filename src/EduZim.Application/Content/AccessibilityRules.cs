using EduZim.Domain.Enums;

namespace EduZim.Application.Content;

/// <summary>Closed captions and transcript requirements (design properties 38, 39).</summary>
public static class AccessibilityRules
{
    public static bool RequiresCaptionTracks(ContentType type, bool hasAudio) =>
        hasAudio && (type == ContentType.Video || type == ContentType.Animation);

    public static bool CaptionRequirementSatisfied(ContentType type, bool hasAudio, int captionTrackCount) =>
        !RequiresCaptionTracks(type, hasAudio) || captionTrackCount >= 1;

    public static bool RequiresTranscript(ContentType type) => type == ContentType.Audio;

    public static bool TranscriptRequirementSatisfied(ContentType type, bool hasTranscriptRecord) =>
        !RequiresTranscript(type) || hasTranscriptRecord;
}
