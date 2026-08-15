using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using FsCheck;
using FsCheck.Fluent;

namespace EduZim.Tests.Properties.Arbitraries;

/// <summary>
/// FsCheck generators for core EduZim domain values used by property tests.
/// Register with <c>[Property(Arbitrary = new[] { typeof(EduZimArbitraries) })]</c>.
/// </summary>
public static class EduZimArbitraries
{
    public static Arbitrary<ApplicationUser> ApplicationUser() => Arb.From(ApplicationUserGen());

    public static Arbitrary<Tenant> Tenant() => Arb.From(TenantGen());

    public static Arbitrary<AssessmentAttempt> AssessmentAttempt() => Arb.From(AssessmentAttemptGen());

    public static Arbitrary<Subscription> Subscription() => Arb.From(SubscriptionGen());

    public static Arbitrary<ContentItem> ContentItem() => Arb.From(ContentItemGen());

    public static Arbitrary<Notification> Notification() => Arb.From(NotificationGen());

    public static Arbitrary<OfflineProgressItem> OfflineProgressItem() => Arb.From(OfflineProgressItemGen());

    private static Gen<Guid> GuidGen() => Gen.Fresh(Guid.NewGuid);

    private static Gen<bool> BoolGen() => Gen.Elements(false, true);

    private static Gen<DateTime> UtcDateTimeGen() =>
        Gen.Choose(0, 365 * 10)
            .Select(days => new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(days));

    private static Gen<string> LabelGen() =>
        from n in Gen.Choose(3, 16)
        from chars in Gen.ArrayOf(Gen.Choose((int)'a', (int)'z').Select(i => (char)i), n)
        select new string(chars);

    private static Gen<TEnum> EnumGen<TEnum>() where TEnum : struct, Enum =>
        Gen.Elements(Enum.GetValues<TEnum>());

    private static Gen<ApplicationUser> ApplicationUserGen() =>
        from id in GuidGen()
        from tenantId in GuidGen()
        from role in EnumGen<UserRole>()
        from font in EnumGen<FontSize>()
        from label in LabelGen()
        from confirmed in BoolGen()
        select new ApplicationUser
        {
            Id = id,
            TenantId = role == UserRole.PlatformAdmin ? null : tenantId,
            Role = role,
            UserName = $"{label}@eduzim.test",
            Email = $"{label}@eduzim.test",
            EmailConfirmed = confirmed,
            FullName = label,
            PreferredLanguage = "en",
            FontSize = font,
        };

    private static Gen<Tenant> TenantGen() =>
        from id in GuidGen()
        from name in LabelGen()
        from tier in EnumGen<TenantTier>()
        from status in EnumGen<TenantStatus>()
        from created in UtcDateTimeGen()
        select new Tenant
        {
            Id = id,
            Name = name,
            Tier = tier,
            Status = status,
            Branding = new BrandingSettings { SchoolName = name, PrimaryColour = "#1976D2" },
            CreatedAt = created,
        };

    private static Gen<AssessmentAttempt> AssessmentAttemptGen() =>
        from id in GuidGen()
        from tenantId in GuidGen()
        from assessmentId in GuidGen()
        from studentId in GuidGen()
        from started in UtcDateTimeGen()
        from score in Gen.Choose(0, 100)
        from timeTaken in Gen.Choose(0, 7200)
        from submitted in BoolGen()
        select new AssessmentAttempt
        {
            Id = id,
            TenantId = tenantId,
            AssessmentId = assessmentId,
            StudentId = studentId,
            StartedAt = started,
            ScorePercent = score,
            TimeTakenSeconds = timeTaken,
            SubmittedAt = submitted ? started.AddMinutes(Math.Max(1, timeTaken / 60)) : null,
            CreatedAt = started,
            UpdatedAt = started,
        };

    private static Gen<Subscription> SubscriptionGen() =>
        from id in GuidGen()
        from tenantId in GuidGen()
        from cycle in EnumGen<BillingCycle>()
        from status in EnumGen<SubscriptionStatus>()
        from start in UtcDateTimeGen()
        from students in Gen.Choose(1, 500)
        select new Subscription
        {
            Id = id,
            TenantId = tenantId,
            Cycle = cycle,
            Status = status,
            CurrentPeriodStart = start,
            CurrentPeriodEnd = start.AddMonths(1),
            StudentCount = students,
        };

    private static Gen<ContentItem> ContentItemGen() =>
        from id in GuidGen()
        from tenantId in GuidGen()
        from title in LabelGen()
        from type in EnumGen<ContentType>()
        from status in EnumGen<ContentStatus>()
        from size in Gen.Choose(1, 50_000_000)
        from uploader in GuidGen()
        from created in UtcDateTimeGen()
        select new ContentItem
        {
            Id = id,
            TenantId = tenantId,
            Title = title,
            Type = type,
            StorageKey = $"content/{id:N}",
            FileSizeBytes = size,
            Language = "en",
            Status = status,
            UploadedByUserId = uploader,
            CreatedAt = created,
            UpdatedAt = created,
        };

    private static Gen<Notification> NotificationGen() =>
        from id in GuidGen()
        from tenantId in GuidGen()
        from userId in GuidGen()
        from type in EnumGen<NotificationType>()
        from channel in EnumGen<NotificationChannel>()
        from status in EnumGen<NotificationStatus>()
        from isRead in BoolGen()
        from retries in Gen.Choose(0, 3)
        from created in UtcDateTimeGen()
        from message in LabelGen()
        select new Notification
        {
            Id = id,
            TenantId = tenantId,
            UserId = userId,
            Type = type,
            Message = message,
            IsRead = isRead,
            Channel = channel,
            Status = status,
            RetryCount = retries,
            CreatedAt = created,
            UpdatedAt = created,
        };

    private static Gen<OfflineProgressItem> OfflineProgressItemGen() =>
        from id in GuidGen()
        from tenantId in GuidGen()
        from studentId in GuidGen()
        from moduleId in GuidGen()
        from completed in BoolGen()
        from timestamp in UtcDateTimeGen()
        from timeOnTask in Gen.Choose(0, 86_400)
        select new OfflineProgressItem
        {
            Id = id,
            TenantId = tenantId,
            StudentId = studentId,
            ModuleId = moduleId,
            IsCompleted = completed,
            LocalTimestamp = timestamp,
            TimeOnTaskSeconds = timeOnTask,
        };
}

public sealed class OfflineProgressItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid ModuleId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LocalTimestamp { get; set; }
    public int TimeOnTaskSeconds { get; set; }
}
