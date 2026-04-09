using EduZim.Domain.Entities;

namespace EduZim.Domain.Privacy;

/// <summary>
/// Requirement 16.4: after a data subject deletion request is processed, PII must be cleared or replaced with tombstone values.
/// Application handlers should call <see cref="Apply"/> when executing an approved deletion job.
/// </summary>
public static class SubjectPiiRedaction
{
    public const string TombstoneDisplayName = "[deleted]";

    /// <summary>
    /// Clears email/phone and replaces the display name with a fixed tombstone. UserName is set to a non-identifying stable value so Identity uniqueness can be preserved.
    /// </summary>
    public static void Apply(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.Email = null;
        user.NormalizedEmail = null;
        user.PhoneNumber = null;
        user.FullName = TombstoneDisplayName;

        var anon = $"deleted-{user.Id:N}";
        user.UserName = anon;
        user.NormalizedUserName = anon.ToUpperInvariant();
    }

    /// <summary>
    /// Predicate used to assert Property 43: no recoverable PII remains after redaction.
    /// </summary>
    public static bool MeetsDeletionRequirement(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Email is not null || user.NormalizedEmail is not null || user.PhoneNumber is not null)
            return false;

        if (user.FullName is not null && user.FullName != TombstoneDisplayName)
            return false;

        return user.UserName?.StartsWith("deleted-", StringComparison.Ordinal) == true;
    }
}
