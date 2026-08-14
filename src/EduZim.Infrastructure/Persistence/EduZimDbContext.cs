using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Infrastructure.Persistence.Configurations;
using EduZim.Infrastructure.Persistence.Encryption;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EduZim.Infrastructure.Persistence;

public class EduZimDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IEduZimDbContext
{
    private readonly IDataProtectionProvider _dataProtectionProvider;

    public EduZimDbContext(DbContextOptions<EduZimDbContext> options, IDataProtectionProvider dataProtectionProvider)
        : base(options)
    {
        _dataProtectionProvider = dataProtectionProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<CaptionTrack> CaptionTracks => Set<CaptionTrack>();
    public DbSet<Transcript> Transcripts => Set<Transcript>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ModuleContentItem> ModuleContentItems => Set<ModuleContentItem>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();
    public DbSet<AnswerRecord> AnswerRecords => Set<AnswerRecord>();
    public DbSet<StudentProgress> StudentProgresses => Set<StudentProgress>();
    public DbSet<SubjectProgress> SubjectProgresses => Set<SubjectProgress>();
    public DbSet<StudentSession> StudentSessions => Set<StudentSession>();
    public DbSet<StudentPoints> StudentPoints => Set<StudentPoints>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<ZimBotInteraction> ZimBotInteractions => Set<ZimBotInteraction>();
    public DbSet<ClassroomSession> ClassroomSessions => Set<ClassroomSession>();
    public DbSet<ClassroomParticipant> ClassroomParticipants => Set<ClassroomParticipant>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<OfflineSyncQueue> OfflineSyncQueues => Set<OfflineSyncQueue>();
    public DbSet<SyncConflictLog> SyncConflictLogs => Set<SyncConflictLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<TenantInviteCode> TenantInviteCodes => Set<TenantInviteCode>();
    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SubscriptionInvoice> SubscriptionInvoices => Set<SubscriptionInvoice>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<ClassEnrollment> ClassEnrollments => Set<ClassEnrollment>();
    public DbSet<AssessmentClassAssignment> AssessmentClassAssignments => Set<AssessmentClassAssignment>();
    public DbSet<ContentPack> ContentPacks => Set<ContentPack>();
    public DbSet<ContentPackItem> ContentPackItems => Set<ContentPackItem>();
    public DbSet<ContentPackAccessRequest> ContentPackAccessRequests => Set<ContentPackAccessRequest>();
    public DbSet<ContentPackRating> ContentPackRatings => Set<ContentPackRating>();

    public Task SetSessionTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        // In-memory EF provider does not support raw SQL; RLS is a PostgreSQL concern only.
        if (Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
            return Task.CompletedTask;

        return Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('app.current_tenant_id', {tenantId.ToString()}, false)",
            cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantConfiguration).Assembly);

        var piiConverter = PiiStringConverter.Create(_dataProtectionProvider);
        var piiComparer = PiiStringConverter.CreateComparer(_dataProtectionProvider);
        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.Email).HasConversion(piiConverter, piiComparer);
            e.Property(u => u.PhoneNumber).HasConversion(piiConverter, piiComparer);
            e.Property(u => u.FullName).HasConversion(piiConverter, piiComparer);
        });
    }
}
