using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Application.Common.Interfaces;

public interface IEduZimDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<ContentItem> ContentItems { get; }
    DbSet<Module> Modules { get; }
    DbSet<ModuleContentItem> ModuleContentItems { get; }
    DbSet<CaptionTrack> CaptionTracks { get; }
    DbSet<Transcript> Transcripts { get; }
    DbSet<TenantInviteCode> TenantInviteCodes { get; }
    DbSet<ParentStudentLink> ParentStudentLinks { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<Payment> Payments { get; }
    DbSet<SubscriptionInvoice> SubscriptionInvoices { get; }
    DbSet<Assessment> Assessments { get; }
    DbSet<Question> Questions { get; }
    DbSet<QuestionOption> QuestionOptions { get; }
    DbSet<AssessmentAttempt> AssessmentAttempts { get; }
    DbSet<AnswerRecord> AnswerRecords { get; }
    DbSet<SchoolClass> SchoolClasses { get; }
    DbSet<ClassEnrollment> ClassEnrollments { get; }
    DbSet<AssessmentClassAssignment> AssessmentClassAssignments { get; }
    DbSet<StudentProgress> StudentProgresses { get; }
    DbSet<SubjectProgress> SubjectProgresses { get; }
    DbSet<StudentPoints> StudentPoints { get; }
    DbSet<Badge> Badges { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }
    DbSet<ZimBotInteraction> ZimBotInteractions { get; }
    DbSet<ClassroomSession> ClassroomSessions { get; }
    DbSet<ClassroomParticipant> ClassroomParticipants { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<OfflineSyncQueue> OfflineSyncQueues { get; }
    DbSet<SyncConflictLog> SyncConflictLogs { get; }
    DbSet<ContentPack> ContentPacks { get; }
    DbSet<ContentPackItem> ContentPackItems { get; }
    DbSet<ContentPackAccessRequest> ContentPackAccessRequests { get; }
    DbSet<ContentPackRating> ContentPackRatings { get; }

    /// <summary>Sets PostgreSQL <c>app.current_tenant_id</c> for row-level security (e.g. Hangfire jobs).</summary>
    Task SetSessionTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
