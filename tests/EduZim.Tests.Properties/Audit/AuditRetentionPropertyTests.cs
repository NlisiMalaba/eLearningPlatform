using EduZim.Domain.Audit;
using FsCheck.Xunit;

namespace EduZim.Tests.Properties.Audit;

// Feature: elearning-app-zimbabwe, Property 45: Audit Log Retention
/// <summary>
/// Feature: elearning-app-zimbabwe, Property 45: Audit Log Retention — for any administrative audit entry,
/// it must not be deleted or modified for at least 12 months from its timestamp (Requirement 16.6).
/// </summary>
public sealed class AuditRetentionPropertyTests
{
    /// <summary>Bounded timestamps so <see cref="DateTimeOffset.AddMonths(int)"/> stays representable.</summary>
    private static DateTimeOffset EntryFromSeed(uint seed)
    {
        var seconds = seed % (uint)(8L * 365 * 24 * 3600);
        return DateTimeOffset.UnixEpoch.AddSeconds(seconds);
    }

    [Property(MaxTest = 100)]
    public void Property45_minimum_eligible_instant_is_twelve_months_after_timestamp(uint seed)
    {
        var entryTimestamp = EntryFromSeed(seed);
        var minimum = AuditLogRetentionPolicy.MinimumUtcEligibleForDeletionOrModification(entryTimestamp);
        Assert.Equal(entryTimestamp.AddMonths(12), minimum);
    }

    [Property(MaxTest = 100)]
    public void Property45_strictly_before_minimum_eligible_deletion_or_modification_forbidden(uint seed)
    {
        var entryTimestamp = EntryFromSeed(seed);
        var minimum = AuditLogRetentionPolicy.MinimumUtcEligibleForDeletionOrModification(entryTimestamp);
        if (minimum == DateTimeOffset.MinValue)
            return;

        var justBefore = minimum.AddTicks(-1);
        Assert.False(AuditLogRetentionPolicy.IsDeletionOrModificationAllowed(entryTimestamp, justBefore));
    }

    [Property(MaxTest = 100)]
    public void Property45_at_minimum_eligible_deletion_or_modification_allowed(uint seed)
    {
        var entryTimestamp = EntryFromSeed(seed);
        var minimum = AuditLogRetentionPolicy.MinimumUtcEligibleForDeletionOrModification(entryTimestamp);
        Assert.True(AuditLogRetentionPolicy.IsDeletionOrModificationAllowed(entryTimestamp, minimum));
    }

    [Property(MaxTest = 100)]
    public void Property45_any_later_utc_allows_deletion_or_modification(uint seed, int extraTicks)
    {
        var entryTimestamp = EntryFromSeed(seed);
        var minimum = AuditLogRetentionPolicy.MinimumUtcEligibleForDeletionOrModification(entryTimestamp);
        var extra = Math.Abs(extraTicks % 1_000_000);
        var later = minimum.AddTicks(extra);
        Assert.True(AuditLogRetentionPolicy.IsDeletionOrModificationAllowed(entryTimestamp, later));
    }
}
