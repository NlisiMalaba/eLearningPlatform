# Design Document: EduZim eLearning Platform

## Overview

EduZim is a Zimbabwe-focused eLearning platform serving two tiers:

- **Pre-school Tier** (ages 2–5): A consumer product subscribed to by parents, delivering foundational learning through 3D animations, audio, games, and AI tutoring.
- **School Tier** (ECD Grade 0 – Grade 7): A multi-tenant B2B product where each subscribing school operates in a fully isolated data environment, with teachers, students, parents, and admins all scoped to their tenant.

The backend is a **single deployable ASP.NET Core (.NET 10+) application** structured using Clean Architecture. The frontend is **React** (web PWA) and **React Native** (mobile). Real-time features use **SignalR**. Background processing uses **Hangfire**. Data is stored in a single **PostgreSQL** database with Row-Level Security (RLS) enforcing multi-tenant isolation. **Entity Framework Core** (`EduZimDbContext`) is the ORM. **Redis** provides distributed caching. **S3-compatible object storage** (MinIO or AWS S3) holds media assets. In-process event handling uses **MediatR** notifications.

---

## Architecture

### Solution Structure

```
EduZim.sln
  src/
    EduZim.Domain/          ← Entities, value objects, domain events, enums, domain interfaces
    EduZim.Application/     ← MediatR commands/queries/handlers, DTOs, validators, app interfaces
    EduZim.Infrastructure/  ← EF Core, repositories, external integrations, Hangfire, Redis, S3, SignalR
    EduZim.API/             ← ASP.NET Core minimal API endpoints, middleware, JWT auth
  clients/
    eduzim-web/             ← React PWA
    eduzim-mobile/          ← React Native
  tests/
    EduZim.Tests.Unit/
    EduZim.Tests.Integration/
    EduZim.Tests.Properties/
```

### Layer Dependency Rules

```
EduZim.API  →  EduZim.Application  →  EduZim.Domain
                      ↑
          EduZim.Infrastructure
```

- **Domain** has zero external dependencies.
- **Application** depends only on Domain.
- **Infrastructure** depends on Application and Domain (implements Application interfaces).
- **API** depends only on Application (dispatches MediatR commands/queries).

### High-Level Architecture

```mermaid
graph TB
    subgraph Clients
        WEB[React Web PWA]
        MOB[React Native Mobile]
    end

    subgraph EduZim.API
        ENDPOINTS[Minimal API Endpoints]
        MW[Middleware\nJWT · Tenant · Audit · Exception]
    end

    subgraph EduZim.Application
        IDENTITY[Identity Handlers]
        TENANTS[Tenant Handlers]
        CONTENT[Content Handlers]
        ASSESS[Assessment Handlers]
        ADAPT[Adaptive Learning Handlers]
        GAMIFY[Gamification Handlers]
        NOTIFY[Notification Handlers]
        ZIMBOT[ZimBot Handlers]
        BILLING[Billing Handlers]
        LIVE[Live Classroom Handlers]
        SYNC[Sync Handlers]
        MARKET[Marketplace Handlers]
        PROGRESS[Progress Handlers]
    end

    subgraph EduZim.Infrastructure
        DBCTX[EduZimDbContext\nEF Core + RLS]
        REPOS[Repositories]
        HANGFIRE[Hangfire Jobs]
        SIGNALR[SignalR Hubs]
        EXTSVCS[External Services\nAI · SMS · Email · Payment · Video · S3]
        REDIS[Redis Cache]
    end

    subgraph Data
        PG[(PostgreSQL + RLS)]
        REDISDB[(Redis)]
        S3[(S3 Object Store)]
    end

    subgraph External
        STRIPE[Stripe / Paynow]
        SMS[SMS Gateway]
        EMAIL[SendGrid / SES]
        AI[OpenAI / Azure OpenAI]
        VIDEO[Daily.co / Jitsi]
    end

    WEB --> ENDPOINTS
    MOB --> ENDPOINTS
    ENDPOINTS --> MW
    MW --> IDENTITY
    MW --> TENANTS
    MW --> CONTENT
    MW --> ASSESS
    MW --> ADAPT
    MW --> GAMIFY
    MW --> NOTIFY
    MW --> ZIMBOT
    MW --> BILLING
    MW --> LIVE
    MW --> MARKET
    MW --> PROGRESS

    IDENTITY --> REPOS
    TENANTS --> REPOS
    CONTENT --> REPOS
    ASSESS --> REPOS
    ADAPT --> REPOS
    ADAPT --> REDIS
    GAMIFY --> REPOS
    NOTIFY --> REPOS
    ZIMBOT --> EXTSVCS
    BILLING --> REPOS
    BILLING --> EXTSVCS
    LIVE --> REPOS
    LIVE --> SIGNALR
    SYNC --> REPOS
    MARKET --> REPOS

    REPOS --> DBCTX
    DBCTX --> PG
    REDIS --> REDISDB
    EXTSVCS --> STRIPE
    EXTSVCS --> SMS
    EXTSVCS --> EMAIL
    EXTSVCS --> AI
    EXTSVCS --> VIDEO
    EXTSVCS --> S3
    HANGFIRE --> PG
```

### Request Flow

```mermaid
sequenceDiagram
    participant Client
    participant API as EduZim.API
    participant MediatR
    participant Handler as Application Handler
    participant Repo as Repository (Infrastructure)
    participant PG as PostgreSQL (RLS)

    Client->>API: HTTPS Request + JWT
    API->>API: JWT middleware validates token
    API->>API: TenantMiddleware extracts tenant_id from claims
    API->>MediatR: Send(command or query)
    MediatR->>Handler: Dispatch to handler
    Handler->>Repo: Repository call
    Repo->>PG: SET app.current_tenant_id; query
    PG->>PG: RLS policy filters rows
    PG-->>Repo: Tenant-scoped results
    Repo-->>Handler: Typed entities
    Handler->>MediatR: Publish domain event notifications (if any)
    Handler-->>API: Result DTO
    API-->>Client: HTTP Response
```

### In-Process Event Handling

Domain events are published via **MediatR notifications** within the same request pipeline or from Hangfire background jobs. This replaces the previous MassTransit + RabbitMQ message bus.

```csharp
// Domain event published after module completion
public record ModuleCompletedNotification(Guid StudentId, Guid ModuleId, Guid TenantId)
    : INotification;

// Multiple handlers respond in-process
public class UpdateLearningProfileOnModuleCompleted
    : INotificationHandler<ModuleCompletedNotification> { ... }

public class AwardPointsOnModuleCompleted
    : INotificationHandler<ModuleCompletedNotification> { ... }

public class UpdateParentDashboardOnModuleCompleted
    : INotificationHandler<ModuleCompletedNotification> { ... }
```

### Multi-Tenancy Strategy

PostgreSQL Row-Level Security (RLS) is the primary enforcement layer. Every school-tier table has a `tenant_id` column. A `SET app.current_tenant_id = '{tenantId}'` session variable is set on every database connection via an EF Core `DbConnectionInterceptor`, and RLS policies filter all reads and writes to that tenant. The application layer also validates tenant claims on every request as a defence-in-depth measure.

---

## Components and Interfaces

The Application layer is organised into feature folders. Each folder contains commands, queries, handlers, DTOs, and validators for that feature domain.

```
EduZim.Application/
  Identity/       Commands: Register, Login, RefreshToken, VerifyEmail, LockAccount, ValidateTwoFactor
  Tenants/        Commands: ProvisionTenant, UpdateBranding, SuspendTenant, GenerateInviteCode
                  Queries:  GetTenantDashboard
  Content/        Commands: UploadContent, CreateModule, ArchiveContent, PublishContent
                  Queries:  GetModuleContent, GetSignedUrl, GetCaptions
  Assessments/    Commands: CreateAssessment, AssignAssessment, SubmitAssessment
                  Queries:  GetAssessmentResult, GetClassResults
  AdaptiveLearning/ Commands: UpdateLearningProfile
                    Queries:  GetRecommendedPath, GetWeeklySummary, GetAdjustedDifficulty
  Gamification/   Commands: AwardPoints, CheckAndAwardBadges
                  Queries:  GetLeaderboard, GetStudentPoints, GetStudentBadges
  Notifications/  Commands: QueueNotification, UpdateNotificationPreferences
                  Queries:  GetInAppNotifications
  ZimBot/         Commands: Chat
                  Queries:  GetInteractionLogs
  Billing/        Commands: CreateSubscription, HandlePaymentSucceeded, HandlePaymentFailed, GenerateInvoice
                  Queries:  GetInvoices, CalculateSchoolFee
  LiveClassrooms/ Commands: ScheduleSession, EndSession
                  Queries:  GetJoinToken, GetAttendanceRecord, GetRecordingUrl
  Sync/           Commands: ProcessOfflineQueue, ResolveConflict
  Marketplace/    Commands: SubmitContentPack, ApproveContentPack, RequestAccess, ApproveAccess, RateContentPack, RemoveContentPack
                  Queries:  BrowseContentPacks
  Progress/       Commands: RecordModuleCompletion
                  Queries:  GetStudentProgress, GetParentDashboard
  Common/         Behaviours: ValidationBehaviour, LoggingBehaviour, TenantScopeBehaviour
                  Interfaces: IRepository<T>, IUnitOfWork, ICurrentUser, IEmailService, ISmsService,
                              IStorageService, IAiService, IPaymentService, IVideoService, ICacheService
```

### Key Application Interfaces (defined in Application, implemented in Infrastructure)

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface ICurrentUser
{
    Guid UserId { get; }
    Guid? TenantId { get; }
    UserRole Role { get; }
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public interface ISmsService
{
    Task<SmsResult> SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}

public interface IStorageService
{
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    Task<string> GetSignedUrlAsync(string key, TimeSpan expiry);
}

public interface IAiService
{
    Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}

public interface IPaymentService
{
    Task<PaymentResult> CreateSubscriptionAsync(CreatePaymentRequest request, CancellationToken ct = default);
    Task<Invoice> GenerateInvoiceAsync(Guid subscriptionId, Guid paymentId, CancellationToken ct = default);
}

public interface IVideoService
{
    Task<string> GetJoinTokenAsync(string roomId, string participantId, CancellationToken ct = default);
    Task<string?> GetRecordingUrlAsync(string roomId, CancellationToken ct = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

### MediatR Pipeline Behaviours (Application/Common/Behaviours)

```csharp
// Runs FluentValidation before every command/query handler
public class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> { ... }

// Logs every command/query with timing
public class LoggingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> { ... }

// Validates tenant claim matches requested resource tenant
public class TenantScopeBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> { ... }
```

---

## Data Models

### Core Entities (EduZim.Domain)

```csharp
// Multi-tenancy base
public abstract class TenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Identity
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    public UserRole Role { get; set; }
    public string? PreferredLanguage { get; set; }
    public bool IsEmailVerified { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
}

public enum UserRole { Student, Teacher, ParentGuardian, SchoolAdmin, PlatformAdmin }

// Tenant
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public TenantTier Tier { get; set; }
    public TenantStatus Status { get; set; }
    public BrandingSettings Branding { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}

public enum TenantTier { PreSchool, School }
public enum TenantStatus { Active, Suspended, Provisioning }

public class BrandingSettings
{
    public string? LogoUrl { get; set; }
    public string SchoolName { get; set; } = default!;
    public string PrimaryColour { get; set; } = "#1976D2";
}

// Subscription
public class Subscription
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public BillingCycle Cycle { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? GracePeriodEnd { get; set; }
    public int? StudentCount { get; set; }
}

public enum BillingCycle { Monthly, Termly, Yearly }
public enum SubscriptionStatus { Active, GracePeriod, Suspended, Cancelled }

// Content
public class ContentItem : TenantEntity
{
    public string Title { get; set; } = default!;
    public ContentType Type { get; set; }
    public string StorageKey { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public int? DurationSeconds { get; set; }
    public string Language { get; set; } = "en";
    public ContentStatus Status { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

public enum ContentType { Video, Pdf, Audio, Scene3D, Animation, Quiz, Game }
public enum ContentStatus { Draft, Published, Archived }

public class Module : TenantEntity
{
    public string Title { get; set; } = default!;
    public GradeLevel Grade { get; set; }
    public string Subject { get; set; } = default!;
    public int SequenceOrder { get; set; }
    public bool IsRequired { get; set; }
    public ICollection<ModuleContentItem> ContentItems { get; set; } = new List<ModuleContentItem>();
}

public enum GradeLevel { EcdGrade0, EcdGrade1, Grade1, Grade2, Grade3, Grade4, Grade5, Grade6, Grade7 }

// Assessment
public class Assessment : TenantEntity
{
    public string Title { get; set; } = default!;
    public Guid ModuleId { get; set; }
    public int? TimeLimitSeconds { get; set; }
    public int PassingScorePercent { get; set; } = 60;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

public class Question
{
    public Guid Id { get; set; }
    public Guid AssessmentId { get; set; }
    public QuestionType Type { get; set; }
    public string Text { get; set; } = default!;
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public string? CorrectAnswer { get; set; }
    public int Points { get; set; }
}

public enum QuestionType { MultipleChoice, TrueFalse, ShortAnswer }

public class AssessmentAttempt : TenantEntity
{
    public Guid AssessmentId { get; set; }
    public Guid StudentId { get; set; }
    public int ScorePercent { get; set; }
    public int TimeTakenSeconds { get; set; }
    public DateTime SubmittedAt { get; set; }
    public ICollection<AnswerRecord> Answers { get; set; } = new List<AnswerRecord>();
}

// Progress
public class StudentProgress : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid ModuleId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TimeOnTaskSeconds { get; set; }
}

// Gamification
public class StudentPoints : TenantEntity
{
    public Guid StudentId { get; set; }
    public int TotalPoints { get; set; }
}

public class Badge : TenantEntity
{
    public Guid StudentId { get; set; }
    public BadgeType Type { get; set; }
    public DateTime EarnedAt { get; set; }
}

public enum BadgeType { FirstModule, FiveConsecutiveDays, SubjectMastery, GradeCompletion }

// Notifications
public class Notification : TenantEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public int RetryCount { get; set; }
}

public enum NotificationType { NewContent, AssessmentDue, BadgeAwarded, LiveClassroomReminder, SubscriptionRenewal, SubscriptionExpiry, InactivityAlert }
public enum NotificationChannel { InApp, Email, Sms }
public enum NotificationStatus { Pending, Delivered, Failed, Undelivered }

// Sync
public class OfflineSyncQueue
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string Payload { get; set; } = default!;
    public DateTime LocalTimestamp { get; set; }
    public SyncStatus Status { get; set; }
}

public enum SyncStatus { Pending, Synced, Conflicted }

// Audit
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = default!;
    public string ResourceType { get; set; } = default!;
    public Guid? ResourceId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}
```

### Database Notes

- A **single** `EduZimDbContext` in `EduZim.Infrastructure` owns all entities.
- All school-tier tables include `tenant_id UUID NOT NULL` with a PostgreSQL RLS policy: `USING (tenant_id = current_setting('app.current_tenant_id')::uuid)`
- PII fields (email, phone, name) are encrypted at the application layer using `Microsoft.AspNetCore.DataProtection` (AES-256) before EF Core persistence.
- `EduZimDbContext` sets the session variable on every connection open via a `DbConnectionInterceptor`.
- Soft deletes use `ArchivedAt` timestamp; a Hangfire job permanently deletes records where `ArchivedAt < NOW() - INTERVAL '30 days'`.

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Cross-Tenant Data Isolation

*For any* API request made by a user authenticated to tenant A, every entity returned in the response must have a `TenantId` equal to tenant A's ID. No request from tenant A should ever return, modify, or reference data belonging to tenant B.

**Validates: Requirements 1.4, 4.7, 7.2, 11.1, 11.3**

### Property 2: Password Storage Never Stores Plaintext

*For any* user registration with any password string P, the value stored in the `PasswordHash` column must not equal P, and two users registering with the same password P must have different stored hash values (due to per-user salting).

**Validates: Requirements 1.8**

### Property 3: Account Lockout Threshold

*For any* user account, submitting exactly 5 consecutive incorrect credential attempts must result in the account being locked. Submitting fewer than 5 consecutive incorrect attempts must not lock the account.

**Validates: Requirements 1.5**

### Property 4: Unverified Accounts Cannot Access Protected Resources

*For any* user account where `IsEmailVerified = false`, all requests to protected API endpoints must be rejected with HTTP 401 or 403.

**Validates: Requirements 1.2**

### Property 5: Subscription Activation on Payment

*For any* payment success event with a valid subscription ID, the subscription's `Status` must transition to `Active` and `CurrentPeriodEnd` must be extended accordingly.

**Validates: Requirements 2.3**

### Property 6: Grace Period on Payment Failure

*For any* payment failure event, the subscription's `Status` must transition to `GracePeriod` (not `Suspended`), and `GracePeriodEnd` must be set to `NOW() + 7 days`.

**Validates: Requirements 2.5**

### Property 7: Subscription Data Preservation

*For any* tenant whose subscription is suspended, all `StudentProgress`, `AssessmentAttempt`, and `Badge` records for that tenant must still exist in the database and must not be deleted for at least 90 days after suspension.

**Validates: Requirements 2.6, 11.6**

### Property 8: Invoice Created for Every Successful Payment

*For any* successful payment event, exactly one `Invoice` record must be created and associated with that payment. The invoice must be retrievable via the billing API.

**Validates: Requirements 2.7**

### Property 9: School Fee Calculation Correctness

*For any* school subscription with N enrolled students and billing cycle C, the calculated total fee must equal `UnitPrice(C) * N`.

**Validates: Requirements 2.8**

### Property 10: Pre-school Video Duration Limit

*For any* content item in the Pre-school Tier with `Type = Video`, its `DurationSeconds` must be less than or equal to 300 (5 minutes).

**Validates: Requirements 3.3**

### Property 11: At Least One Game Per Foundational Concept

*For any* foundational concept category in the Pre-school Tier content catalog, there must exist at least one `ContentItem` with `Type = Game` associated with that concept.

**Validates: Requirements 3.5**

### Property 12: Session Inactivity Pause

*For any* active toddler session, if no interaction events are recorded for 60 consecutive seconds, the session's `Status` must transition to `Paused`.

**Validates: Requirements 3.7**

### Property 13: Module Completion Unlocks Next Module

*For any* student and any module M in a sequence, when the student's `StudentProgress` record for M has `IsCompleted = true`, the next module in the sequence must become accessible to that student.

**Validates: Requirements 4.4**

### Property 14: Progress Percentage Calculation

*For any* student and subject, the displayed progress percentage must equal `(count of completed modules in subject / total modules in subject) * 100`, rounded to the nearest integer.

**Validates: Requirements 4.8**

### Property 15: Remedial Content Recommendation Below 60%

*For any* assessment attempt where `ScorePercent < 60`, the adaptive learning handler must include at least one remedial content recommendation for the associated topic in the student's learning path.

**Validates: Requirements 6.2**

### Property 16: Advanced Extension Offer Above 85%

*For any* assessment attempt where `ScorePercent >= 85`, the adaptive learning handler must include at least one advanced extension activity recommendation for the associated topic.

**Validates: Requirements 6.3**

### Property 17: No Grade Advancement Without Passing Score

*For any* student, the next grade-level module must not be unlocked unless all required assessments for the current module have at least one attempt with `ScorePercent >= 60`.

**Validates: Requirements 6.6**

### Property 18: File Upload Size Enforcement

*For any* content upload request, files exceeding the type-specific size limit (500 MB for video, 50 MB for audio) must be rejected before storage. Files within the limit must be accepted.

**Validates: Requirements 7.1**

### Property 19: Soft Delete Retention

*For any* content item that has been deleted (soft-deleted), its `Status` must be `Archived` immediately after deletion, and no permanent deletion must occur until `ArchivedAt + 30 days` has elapsed.

**Validates: Requirements 7.4**

### Property 20: Assessment Assignment Notifies All Enrolled Students

*For any* class assignment of an assessment, every student enrolled in that class must have a corresponding `Notification` record of type `AssessmentDue` created.

**Validates: Requirements 7.6**

### Property 21: Marketplace Submission Requires Admin Review

*For any* content pack submitted to the marketplace, its `Status` must be `PendingReview` until a Platform Admin explicitly approves it. No pack with `Status != Approved` must appear in marketplace discovery results.

**Validates: Requirements 8.2**

### Property 22: Cross-Tenant Content Access Requires Approval

*For any* cross-tenant content access request, the requesting tenant must not be able to retrieve the content until the originating teacher's approval is recorded. After approval, only the specific content pack must be accessible — no other data from the originating tenant.

**Validates: Requirements 8.3, 8.4**

### Property 23: Marketplace Attribution Completeness

*For any* content pack returned from the marketplace query, the response must include both `SchoolName` and `TeacherName` attribution fields with non-empty values.

**Validates: Requirements 8.5**

### Property 24: Assessment Attempt Record Completeness

*For any* assessment submission, the resulting `AssessmentAttempt` record must contain `ScorePercent`, `TimeTakenSeconds`, and `SubmittedAt` with non-null values.

**Validates: Requirements 9.1**

### Property 25: Points Awarded on Module and Assessment Completion

*For any* module or assessment completion event, the student's `TotalPoints` must increase by at least the base award amount. For assessment scores >= 85%, the increase must include the bonus multiplier.

**Validates: Requirements 9.3**

### Property 26: Badge Awarded on Milestone Events

*For any* milestone event (first module, 5 consecutive days, subject mastery, grade completion), a corresponding `Badge` record must be created for the student, and a certificate generation job must be queued.

**Validates: Requirements 9.4, 9.6**

### Property 27: Leaderboard Tenant Isolation

*For any* leaderboard query by a user in tenant T, all returned `LeaderboardEntry` records must have `TenantId = T`. No entries from other tenants must appear.

**Validates: Requirements 9.5**

### Property 28: Timed Assessment Auto-Submit

*For any* timed assessment where `TimeLimitSeconds` is set, any submission attempt received after `StartedAt + TimeLimitSeconds` must be rejected or auto-submitted with answers recorded up to the time limit.

**Validates: Requirements 9.8**

### Property 29: Parent Dashboard Data Completeness

*For any* parent/guardian with linked students, the dashboard response must include `CurrentGrade`, `Subjects`, `RecentActivity`, and `OverallProgressPercent` for each linked student.

**Validates: Requirements 10.1**

### Property 30: Parent Invite Code Round Trip

*For any* valid invite code generated for a tenant, linking a parent account using that code must succeed and create a parent-student association. Using an expired or invalid code must fail.

**Validates: Requirements 10.3**

### Property 31: Screen Time Limit Enforcement

*For any* student account with a `DailyScreenTimeLimitSeconds` set, once the student's session time for the day reaches that limit, the session must be paused and further session creation must be blocked until the next day.

**Validates: Requirements 10.5**

### Property 32: Inactivity Notification After 7 Days

*For any* student whose `LastLoginAt` is more than 7 days in the past, a notification of type `InactivityAlert` must be queued for each linked parent/guardian.

**Validates: Requirements 10.6**

### Property 33: Offline Sync Conflict Resolution by Timestamp

*For any* sync conflict between a locally queued `OfflineProgressItem` and a server-side `ProgressRecord` for the same student and module, the record with the later `Timestamp` must be retained, and a `SyncConflict` log entry must be created.

**Validates: Requirements 12.5**

### Property 34: Live Classroom Notification Lead Time

*For any* scheduled live classroom session, notification records for all invited students and their parents must be created with a scheduled delivery time of at least `SessionStartTime - 24 hours`.

**Validates: Requirements 13.2**

### Property 35: Attendance Record on Session End

*For any* live classroom session that has ended, an `AttendanceRecord` must exist containing an entry for each participant who joined, with their join time and duration.

**Validates: Requirements 13.5**

### Property 36: Recording Availability Window

*For any* ended live classroom session, the recording URL must be accessible for exactly 30 days after `SessionEndTime`. After 30 days, the URL must return 404 or be expired.

**Validates: Requirements 13.7**

### Property 37: Font Size Preference Validation

*For any* font size preference update, only the values `Small`, `Medium`, `Large`, and `ExtraLarge` must be accepted. Any other value must be rejected with a validation error.

**Validates: Requirements 14.3**

### Property 38: Closed Captions Required for Video Content

*For any* video or animation `ContentItem` that has `HasAudio = true`, at least one `CaptionTrack` record must exist for that content item.

**Validates: Requirements 14.4**

### Property 39: Audio Content Transcript Required

*For any* `ContentItem` with `Type = Audio`, a `Transcript` record must exist and be retrievable from the same content endpoint.

**Validates: Requirements 14.6**

### Property 40: Notification Routing Respects User Preferences

*For any* notification generated for a user, the notification must only be dispatched via channels that the user has enabled in their `NotificationPreferences`. Disabled channels must not receive the notification.

**Validates: Requirements 15.4**

### Property 41: SMS Retry Logic

*For any* failed SMS notification, the `RetryCount` must increment on each retry attempt and must not exceed 3. After 3 failed retries, the notification `Status` must be set to `Undelivered`.

**Validates: Requirements 15.5**

### Property 42: PII Encryption at Rest

*For any* `ApplicationUser` record, the values stored in the `Email`, `PhoneNumber`, and `FullName` database columns must not equal the plaintext values provided during registration (i.e., they must be encrypted).

**Validates: Requirements 16.2**

### Property 43: PII Deletion on Request

*For any* data deletion request, after the deletion job is processed, all PII fields for the associated student must be null or replaced with a tombstone value, and a deletion confirmation notification must be queued.

**Validates: Requirements 16.4**

### Property 44: Unauthorized Requests Return 403 with Audit Log

*For any* API request that fails authorisation (wrong tenant, insufficient role, or missing claim), the HTTP response must be 403, and an `AuditLog` entry must be created recording the attempted access.

**Validates: Requirements 16.5**

### Property 45: Audit Log Retention

*For any* `AuditLog` entry created for an administrative action, that entry must not be deleted or modified for at least 12 months from its `Timestamp`.

**Validates: Requirements 16.6**

---

## Error Handling

### Strategy

All errors follow a consistent pattern using ASP.NET Core's `ProblemDetails` (RFC 7807). The global exception handler in `EduZim.API` maps domain and application exceptions to appropriate HTTP status codes.

```csharp
// Domain exceptions (EduZim.Domain/Exceptions)
public class DomainException : Exception { ... }
public class TenantAccessViolationException : DomainException { ... }

// Application exceptions (EduZim.Application/Exceptions)
public class NotFoundException : Exception { ... }
public class ValidationException : Exception { ... }
public class ConflictException : Exception { ... }

// Global exception handler in EduZim.API/Program.cs
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        // Maps DomainException → 400/403, NotFoundException → 404,
        // ValidationException → 400, ConflictException → 409,
        // unhandled → 500 with generic ProblemDetails
    });
});
```

### Error Categories

| Scenario | HTTP Status | Layer |
|---|---|---|
| Unauthenticated request | 401 | API middleware |
| Unauthorised (wrong tenant/role) | 403 | TenantScopeBehaviour + audit log |
| Validation failure | 400 | ValidationBehaviour (FluentValidation) |
| Resource not found | 404 | Application handler throws `NotFoundException` |
| Conflict (duplicate, version mismatch) | 409 | Application handler throws `ConflictException` |
| File too large | 413 | API middleware rejects before dispatching command |
| External service unavailable | 503 | Infrastructure throws; Polly circuit breaker returns fallback |
| Unhandled exception | 500 | Global handler logs + returns generic ProblemDetails |

### Resilience Patterns

- **Polly** retry and circuit breaker policies on all `HttpClient` instances in Infrastructure (AI service, SMS gateway, payment provider, video provider).
- **ZimBot fallback**: when the AI circuit is open, the `IAiService` implementation returns a static fallback message and logs the question via MediatR notification.
- **SMS retry**: Hangfire job retries failed SMS up to 3 times at 10-minute intervals; marks `Undelivered` after 3 failures.
- **Sync conflict**: `ProcessOfflineQueueHandler` uses last-write-wins by timestamp; conflicts are logged to `SyncConflictLog`.
- **Idempotency**: Payment webhook handlers use idempotency keys to prevent double-processing of payment events.

### Tenant Suspension

When a tenant's subscription is suspended, `TenantMiddleware` in `EduZim.API` intercepts all requests for that tenant and returns:

```json
{
  "type": "https://eduzim.co.zw/errors/tenant-suspended",
  "title": "Subscription Suspended",
  "status": 402,
  "detail": "Your school's subscription has expired. Please contact your administrator."
}
```

Data is preserved for 90 days; a Hangfire job schedules permanent deletion at `SuspendedAt + 90 days`.

---

## Testing Strategy

### Dual Testing Approach

EduZim uses both unit/integration tests and property-based tests. They are complementary:

- **Unit/Integration tests** (xUnit): verify specific examples, edge cases, error conditions, and integration points between components.
- **Property-based tests** (FsCheck): verify universal properties across randomly generated inputs, providing confidence that correctness holds for all valid inputs.

### Property-Based Testing with FsCheck

**Library**: [FsCheck](https://fscheck.github.io/FsCheck/) — the standard property-based testing library for .NET, usable from C# via `FsCheck.Xunit`.

**Configuration**: Each property test must run a minimum of **100 iterations** (increase to 500 for critical security properties).

```csharp
// Example property test in EduZim.Tests.Properties
[Property(MaxTest = 500, Arbitrary = new[] { typeof(EduZimArbitraries) })]
public Property TenantIsolation_UserInTenantA_CannotSeeDataFromTenantB(
    Guid tenantAId, Guid tenantBId, ApplicationUser userInA)
{
    // Feature: elearning-app-zimbabwe, Property 1: Cross-Tenant Data Isolation
    return (tenantAId != tenantBId).Implies(() =>
    {
        var result = _tenantScopedRepository.GetData(userInA, tenantBId);
        return result.IsEmpty;
    });
}
```

**Tag format for all property tests**:
```
// Feature: elearning-app-zimbabwe, Property {N}: {property_text}
```

### Property Test Coverage Map

| Property | Test Class | FsCheck Property Method |
|---|---|---|
| P1: Cross-Tenant Isolation | `TenantIsolationTests` | `UserInTenantA_CannotSeeDataFromTenantB` |
| P2: Password Never Plaintext | `IdentityHandlerTests` | `StoredHash_NeverEqualsPlaintext` |
| P3: Account Lockout Threshold | `IdentityHandlerTests` | `FiveFailures_LocksAccount` |
| P4: Unverified Account Blocked | `IdentityHandlerTests` | `UnverifiedAccount_CannotAccessProtectedEndpoints` |
| P5: Subscription Activation | `BillingHandlerTests` | `PaymentSuccess_ActivatesSubscription` |
| P6: Grace Period on Failure | `BillingHandlerTests` | `PaymentFailure_EntersGracePeriod` |
| P7: Data Preserved on Suspension | `BillingHandlerTests` | `SuspendedTenant_DataPreservedFor90Days` |
| P8: Invoice Per Payment | `BillingHandlerTests` | `SuccessfulPayment_CreatesInvoice` |
| P9: School Fee Calculation | `BillingHandlerTests` | `FeeCalculation_IsLinearInStudentCount` |
| P10: Video Duration Limit | `ContentHandlerTests` | `PreschoolVideo_DurationUnder5Minutes` |
| P11: Game Per Concept | `ContentHandlerTests` | `EachFoundationalConcept_HasAtLeastOneGame` |
| P12: Session Inactivity Pause | `SessionHandlerTests` | `NoInteraction60Seconds_PausesSession` |
| P13: Module Unlock on Completion | `ProgressHandlerTests` | `ModuleCompletion_UnlocksNextModule` |
| P14: Progress Percentage | `ProgressHandlerTests` | `ProgressPercent_EqualsCompletedOverTotal` |
| P15: Remedial Below 60% | `AdaptiveLearningHandlerTests` | `ScoreBelow60_RecommendsRemedial` |
| P16: Advanced Above 85% | `AdaptiveLearningHandlerTests` | `ScoreAbove85_OffersAdvancedExtension` |
| P17: No Advancement Without Pass | `AdaptiveLearningHandlerTests` | `NextModule_NotUnlockedWithoutPassingScore` |
| P18: File Size Enforcement | `ContentHandlerTests` | `OversizedFile_IsRejected` |
| P19: Soft Delete Retention | `ContentHandlerTests` | `DeletedContent_RemainsArchivedFor30Days` |
| P20: Assessment Notifies All Students | `AssessmentHandlerTests` | `AssignAssessment_NotifiesAllEnrolledStudents` |
| P21: Marketplace Review Gate | `MarketplaceHandlerTests` | `SubmittedPack_NotDiscoverableUntilApproved` |
| P22: Cross-Tenant Access Requires Approval | `MarketplaceHandlerTests` | `ContentAccess_RequiresOriginatingTeacherApproval` |
| P23: Marketplace Attribution | `MarketplaceHandlerTests` | `MarketplacePack_ContainsAttribution` |
| P24: Attempt Record Completeness | `AssessmentHandlerTests` | `Submission_CreatesCompleteAttemptRecord` |
| P25: Points on Completion | `GamificationHandlerTests` | `Completion_AwardsPoints` |
| P26: Badge on Milestone | `GamificationHandlerTests` | `MilestoneEvent_AwardsBadgeAndCertificate` |
| P27: Leaderboard Tenant Isolation | `GamificationHandlerTests` | `Leaderboard_OnlyContainsTenantStudents` |
| P28: Timed Assessment Auto-Submit | `AssessmentHandlerTests` | `LateSubmission_IsRejectedOrAutoSubmitted` |
| P29: Parent Dashboard Completeness | `ProgressHandlerTests` | `Dashboard_ContainsAllRequiredFields` |
| P30: Invite Code Round Trip | `TenantHandlerTests` | `ValidInviteCode_LinksParentToStudent` |
| P31: Screen Time Enforcement | `SessionHandlerTests` | `ScreenTimeLimit_PausesSessionWhenReached` |
| P32: Inactivity Notification | `NotificationHandlerTests` | `StudentInactive7Days_NotifiesParent` |
| P33: Sync Conflict Resolution | `SyncHandlerTests` | `Conflict_LaterTimestampWins` |
| P34: Classroom Notification Lead Time | `LiveClassroomHandlerTests` | `ScheduledSession_NotificationAt24HoursBefore` |
| P35: Attendance Record on End | `LiveClassroomHandlerTests` | `SessionEnd_CreatesAttendanceRecord` |
| P36: Recording Availability Window | `LiveClassroomHandlerTests` | `Recording_AvailableFor30DaysOnly` |
| P37: Font Size Validation | `UserPreferencesHandlerTests` | `InvalidFontSize_IsRejected` |
| P38: Captions for Video | `ContentHandlerTests` | `VideoWithAudio_HasCaptionTrack` |
| P39: Audio Transcript Required | `ContentHandlerTests` | `AudioContent_HasTranscript` |
| P40: Notification Preference Routing | `NotificationHandlerTests` | `DisabledChannel_DoesNotReceiveNotification` |
| P41: SMS Retry Logic | `NotificationHandlerTests` | `FailedSms_RetriesUpTo3Times` |
| P42: PII Encryption | `IdentityHandlerTests` | `StoredPii_IsEncrypted` |
| P43: PII Deletion | `IdentityHandlerTests` | `DeletionRequest_RemovesPii` |
| P44: 403 with Audit Log | `AuthorisationTests` | `UnauthorisedRequest_Returns403AndLogsAudit` |
| P45: Audit Log Retention | `AuditHandlerTests` | `AuditLog_NotDeletedBefore12Months` |

### Unit and Integration Test Strategy

**Unit tests** (xUnit, Moq for mocking):
- Application handler logic with mocked repositories and infrastructure interfaces
- FluentValidation validators
- Domain model invariants and value objects
- Calculation functions (fee calculation, progress percentage, points)

**Integration tests** (xUnit + `WebApplicationFactory<Program>` + Testcontainers for PostgreSQL):
- Full HTTP request/response cycle through the ASP.NET Core pipeline
- `EduZimDbContext` queries against real PostgreSQL (Testcontainers)
- RLS policy enforcement verified with real database connections
- Hangfire job scheduling and execution

**Test project structure**:
```
tests/
  EduZim.Tests.Unit/
    Handlers/          ← Application handler unit tests (mocked dependencies)
    Validators/        ← FluentValidation unit tests
    Domain/            ← Domain model invariant tests
  EduZim.Tests.Integration/
    Api/               ← WebApplicationFactory end-to-end tests
    Database/          ← Testcontainers RLS and EF Core tests
  EduZim.Tests.Properties/
    Arbitraries/       ← FsCheck custom generators
    Identity/
    Billing/
    Content/
    Assessment/
    Gamification/
    Notification/
    Sync/
    Marketplace/
    LiveClassroom/
    Progress/
```

**Running tests**:
```bash
# All tests (single run)
dotnet test --no-watch

# Property tests only
dotnet test --filter "Category=Property"

# Integration tests only
dotnet test --filter "Category=Integration"
```
