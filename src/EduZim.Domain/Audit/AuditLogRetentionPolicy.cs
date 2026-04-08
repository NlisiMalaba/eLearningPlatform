namespace EduZim.Domain.Audit;

/// <summary>
/// Requirement 16.6: administrative audit entries must not be deleted or modified for at least 12 months from the entry timestamp.
/// </summary>
public static class AuditLogRetentionPolicy
{
    /// <summary>Earliest UTC instant at which an entry may be purged or altered.</summary>
    public static DateTimeOffset MinimumUtcEligibleForDeletionOrModification(DateTimeOffset entryTimestampUtc)
    {
        return entryTimestampUtc.AddMonths(12);
    }

    /// <summary>Returns whether the current time is at or after the end of the minimum retention window.</summary>
    public static bool IsDeletionOrModificationAllowed(DateTimeOffset entryTimestampUtc, DateTimeOffset utcNow)
    {
        return utcNow >= MinimumUtcEligibleForDeletionOrModification(entryTimestampUtc);
    }
}
