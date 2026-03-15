# Implementation Plan: EduZim eLearning Platform

## Overview

Implement the EduZim platform as 12 ASP.NET Core microservices, a React Web PWA, and a React Native mobile app, backed by PostgreSQL with RLS, Redis, RabbitMQ, and S3-compatible storage. Tasks are sequenced: shared infrastructure → core services → feature services → frontends → integration wiring → testing.

## Tasks

- [ ] 1. Scaffold solution structure and shared infrastructure
  - Create the .NET solution file with all 12 service projects, shared library projects, and test projects
  - Add `EduZim.Shared` class library with `TenantEntity` base class, `UserRole` enum, `ProblemDetails` helpers, and `AuditLog` entity
  - Configure `docker-compose.yml` with PostgreSQL, Redis, RabbitMQ, and MinIO containers for local development
  - _Requirements: 11.1, 11.2, 16.1_

- [ ] 2. PostgreSQL schema, RLS policies, and EF Core setup
  - [ ] 2.1 Create EF Core `DbContext` classes for each service with all entities from the data model
    - Implement `TenantDbContext` base that sets `app.current_tenant_id` session variable on every connection open via a `DbConnectionInterceptor`
    - Add all school-tier tables with `tenant_id UUID NOT NULL` columns
    - _Requirements: 11.1, 11.3_
  - [ ] 2.2 Write EF Core migrations for all service databases
    - Include RLS policy SQL in migrations: `USING (tenant_id = current_setting('app.current_tenant_id')::uuid)`
    - Enable RLS on all school-tier tables via `ALTER TABLE ... ENABLE ROW LEVEL SECURITY`
    - _Requirements: 11.2, 11.3_
  - [ ]* 2.3 Write property test for cross-tenant data isolation (Property 1)
    - **Property 1: Cross-Tenant Data Isolation**
    - **Validates: Requirements 1.4, 4.7, 7.2, 11.1, 11.3**

- [ ] 3. YARP API Gateway
  - Create `EduZim.Gateway` ASP.NET Core project with YARP reverse proxy
  - Configure route mappings for all 12 services in `appsettings.json`
  - Add JWT validation middleware that extracts `tenant_id` claim and forwards it as a request header
  - Add tenant-suspension middleware that returns HTTP 402 for suspended tenants
  - _Requirements: 11.3, 16.5_

- [ ] 4. MassTransit + RabbitMQ event bus setup
  - Add `EduZim.Messaging` shared library with all domain event message contracts (`ModuleCompleted`, `AssessmentSubmitted`, `BadgeAwarded`, `PaymentSucceeded`, `PaymentFailed`, `TenantSuspended`, `StudentInactive`)
  - Configure MassTransit with RabbitMQ transport in each service that publishes or consumes events
  - _Requirements: 9.3, 9.4, 10.2, 15.1_

- [ ] 5. Redis distributed cache setup
  - Add `IDistributedCache` (StackExchange.Redis) configuration to `EduZim.Shared`
  - Implement `ICacheService` wrapper with typed get/set/invalidate helpers
  - Wire Redis into Adaptive Learning Service and Identity Service
  - _Requirements: 11.7_

- [ ] 6. Identity Service
  - [ ] 6.1 Implement `IIdentityService` with ASP.NET Identity + OpenIddict
    - Implement `RegisterAsync`, `LoginAsync`, `RefreshTokenAsync`, `RevokeTokenAsync`, `VerifyEmailAsync`
    - Implement per-user salted password hashing via ASP.NET Identity's `IPasswordHasher<ApplicationUser>`
    - Implement PII encryption at rest using `Microsoft.AspNetCore.DataProtection` (AES-256) for email, phone, and name fields before EF Core persistence
    - _Requirements: 1.1, 1.2, 1.8, 16.2_
  - [ ] 6.2 Implement account lockout and 2FA
    - Implement `LockAccountAsync` triggered after 5 consecutive failed logins; set lockout for 15 minutes and queue email notification
    - Implement `ValidateTwoFactorAsync` for Platform Admin data export confirmation
    - _Requirements: 1.5, 16.7_
  - [ ] 6.3 Implement SSO redirect for tenant IdP
    - Implement `POST /auth/sso/{tenantId}` that redirects to the tenant's configured external identity provider
    - _Requirements: 1.7_
  - [ ]* 6.4 Write property tests for Identity Service (Properties 2, 3, 4, 42, 43)
    - **Property 2: Password Storage Never Stores Plaintext — Validates: Requirements 1.8**
    - **Property 3: Account Lockout Threshold — Validates: Requirements 1.5**
    - **Property 4: Unverified Accounts Cannot Access Protected Resources — Validates: Requirements 1.2**
    - **Property 42: PII Encryption at Rest — Validates: Requirements 16.2**
    - **Property 43: PII Deletion on Request — Validates: Requirements 16.4**

- [ ] 7. Authorisation middleware and audit logging
  - Implement `TenantAuthorizationMiddleware` that validates the authenticated user's `tenant_id` claim matches the requested resource's tenant on every request; return HTTP 403 on mismatch
  - Implement `AuditLogService` that writes an `AuditLog` record for every failed authorisation and every administrative action
  - Register both as ASP.NET Core middleware in all service pipelines
  - _Requirements: 11.3, 16.5, 16.6_
  - [ ]* 7.1 Write property tests for authorisation and audit (Properties 44, 45)
    - **Property 44: Unauthorized Requests Return 403 with Audit Log — Validates: Requirements 16.5**
    - **Property 45: Audit Log Retention — Validates: Requirements 16.6**

- [ ] 8. Checkpoint — core infrastructure
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Tenant Service
  - [ ] 9.1 Implement `ITenantService` with tenant provisioning and branding
    - Implement `ProvisionTenantAsync` that creates the tenant record and sets `Status = Provisioning` then `Active` once the isolated data partition is ready
    - Implement `UpdateBrandingAsync` for logo, school name, and colour scheme
    - Implement `GetDashboardAsync` returning enrolled student count, active teacher count, subscription status, and storage usage
    - _Requirements: 1.3, 11.2, 11.4, 11.5_
  - [ ] 9.2 Implement invite code generation and parent-student linking
    - Implement `GenerateInviteCodeAsync` producing a unique, time-limited code scoped to the tenant
    - Implement the invite code redemption endpoint that creates the parent-student association
    - _Requirements: 10.3_
  - [ ] 9.3 Implement tenant suspension and restoration
    - Implement `SuspendTenantAsync` and `RestoreTenantAsync`; schedule a Hangfire job to permanently delete data at `SuspendedAt + 90 days`
    - _Requirements: 2.6, 11.6_
  - [ ]* 9.4 Write property tests for Tenant Service (Properties 30, 7)
    - **Property 30: Parent Invite Code Round Trip — Validates: Requirements 10.3**
    - **Property 7: Subscription Data Preservation — Validates: Requirements 2.6, 11.6**

- [ ] 10. Billing Service
  - [ ] 10.1 Implement `IBillingService` with subscription creation and fee calculation
    - Implement `CreateSubscriptionAsync` for monthly, termly, and yearly cycles (Pre-school and School tiers)
    - Implement `CalculateSchoolFeeAsync` as `UnitPrice(cycle) * studentCount`
    - _Requirements: 2.1, 2.2, 2.8_
  - [ ] 10.2 Implement payment webhook handlers and invoice generation
    - Implement `HandlePaymentSucceededAsync`: transition subscription to `Active`, extend `CurrentPeriodEnd`, generate invoice — all within a single database transaction
    - Implement `HandlePaymentFailedAsync`: transition to `GracePeriod`, set `GracePeriodEnd = NOW() + 7 days`, queue subscriber notification
    - Implement `GenerateInvoiceAsync` producing a downloadable PDF invoice record
    - Use idempotency keys on webhook handlers to prevent double-processing
    - _Requirements: 2.3, 2.5, 2.7_
  - [ ] 10.3 Implement renewal reminder Hangfire jobs
    - Schedule a recurring Hangfire job that queries subscriptions expiring within 7 days and queues email + SMS renewal reminders
    - _Requirements: 2.4_
  - [ ]* 10.4 Write property tests for Billing Service (Properties 5, 6, 7, 8, 9)
    - **Property 5: Subscription Activation on Payment — Validates: Requirements 2.3**
    - **Property 6: Grace Period on Payment Failure — Validates: Requirements 2.5**
    - **Property 7: Subscription Data Preservation — Validates: Requirements 2.6, 11.6**
    - **Property 8: Invoice Created for Every Successful Payment — Validates: Requirements 2.7**
    - **Property 9: School Fee Calculation Correctness — Validates: Requirements 2.8**

- [ ] 11. Content Service
  - [ ] 11.1 Implement file upload with size validation and S3 storage
    - Implement `UploadAsync` with multipart upload to S3-compatible storage; enforce 500 MB limit for video and 50 MB for audio before writing to storage
    - Generate and store signed URLs via `GetSignedUrlAsync`
    - _Requirements: 7.1_
  - [ ] 11.2 Implement module and content lifecycle management
    - Implement `CreateModuleAsync` with grade level, subject, and sequence ordering
    - Implement soft-delete (`ArchiveContentAsync`) setting `Status = Archived` and `ArchivedAt = NOW()`
    - Schedule a Hangfire job to permanently delete records where `ArchivedAt < NOW() - 30 days`
    - _Requirements: 7.2, 7.3, 7.4_
  - [ ] 11.3 Implement captions and transcripts
    - Add `CaptionTrack` and `Transcript` entities; implement endpoints `GET /content/{id}/captions` and transcript retrieval
    - _Requirements: 14.4, 14.6_
  - [ ]* 11.4 Write property tests for Content Service (Properties 10, 11, 18, 19, 38, 39)
    - **Property 10: Pre-school Video Duration Limit — Validates: Requirements 3.3**
    - **Property 11: At Least One Game Per Foundational Concept — Validates: Requirements 3.5**
    - **Property 18: File Upload Size Enforcement — Validates: Requirements 7.1**
    - **Property 19: Soft Delete Retention — Validates: Requirements 7.4**
    - **Property 38: Closed Captions Required for Video Content — Validates: Requirements 14.4**
    - **Property 39: Audio Content Transcript Required — Validates: Requirements 14.6**

- [ ] 12. Assessment Service
  - [ ] 12.1 Implement assessment creation and class assignment
    - Implement `CreateAsync` supporting multiple-choice, true/false, and short-answer question types with optional `TimeLimitSeconds`
    - Implement `AssignToClassAsync`; publish a `AssessmentAssigned` event consumed by Notification Service to notify all enrolled students
    - _Requirements: 7.5, 7.6_
  - [ ] 12.2 Implement submission, auto-grading, and timed enforcement
    - Implement `SubmitAsync` that calculates `ScorePercent`, records `TimeTakenSeconds` and `SubmittedAt`, and returns per-question feedback
    - Enforce time limit: reject submissions where `NOW() > StartedAt + TimeLimitSeconds`; auto-submit via a Hangfire job scheduled at session start
    - Publish `AssessmentSubmitted` event after every submission
    - _Requirements: 9.1, 9.2, 9.8_
  - [ ] 12.3 Implement teacher results dashboard endpoint
    - Implement `GetClassResultsAsync` returning per-student scores, completion rates, and time-on-task
    - _Requirements: 7.7_
  - [ ]* 12.4 Write property tests for Assessment Service (Properties 20, 24, 28)
    - **Property 20: Assessment Assignment Notifies All Enrolled Students — Validates: Requirements 7.6**
    - **Property 24: Assessment Attempt Record Completeness — Validates: Requirements 9.1**
    - **Property 28: Timed Assessment Auto-Submit — Validates: Requirements 9.8**

- [ ] 13. Adaptive Learning Service
  - [ ] 13.1 Implement learning profile update and difficulty adjustment
    - Implement `UpdateLearningProfileAsync` consuming `AssessmentSubmitted` events; update the student's cached learning profile in Redis
    - Implement `GetAdjustedDifficultyAsync` that decrements difficulty in increments when the student is on a remedial path until score >= 70%
    - _Requirements: 6.1, 6.5_
  - [ ] 13.2 Implement recommended learning path generation
    - Implement `GetRecommendedPathAsync`: include remedial content when `ScorePercent < 60`; include advanced extension when `ScorePercent >= 85`; block next grade-level module until all required assessments pass with >= 60%
    - _Requirements: 6.2, 6.3, 6.6_
  - [ ] 13.3 Implement weekly summary generation
    - Implement `GenerateWeeklySummaryAsync` as a Hangfire recurring job; make summary visible to teacher and parent
    - _Requirements: 6.4_
  - [ ]* 13.4 Write property tests for Adaptive Learning Service (Properties 15, 16, 17)
    - **Property 15: Remedial Content Recommendation Below 60% — Validates: Requirements 6.2**
    - **Property 16: Advanced Extension Offer Above 85% — Validates: Requirements 6.3**
    - **Property 17: No Grade Advancement Without Passing Score — Validates: Requirements 6.6**

- [ ] 14. Gamification Service
  - [ ] 14.1 Implement points awarding
    - Implement `AwardPointsAsync` consuming `ModuleCompleted` and `AssessmentSubmitted` events; apply bonus multiplier for scores >= 85%
    - _Requirements: 9.3_
  - [ ] 14.2 Implement badge awarding and certificate generation
    - Implement `CheckAndAwardBadgesAsync` for all four milestone types: `FirstModule`, `FiveConsecutiveDays`, `SubjectMastery`, `GradeCompletion`
    - On badge award, queue a certificate generation job and publish a `BadgeAwarded` event consumed by Notification Service
    - _Requirements: 9.4, 9.6_
  - [ ] 14.3 Implement tenant-scoped leaderboard
    - Implement `GetLeaderboardAsync` with RLS ensuring only students from the requesting tenant appear
    - _Requirements: 9.5_
  - [ ]* 14.4 Write property tests for Gamification Service (Properties 25, 26, 27)
    - **Property 25: Points Awarded on Module and Assessment Completion — Validates: Requirements 9.3**
    - **Property 26: Badge Awarded on Milestone Events — Validates: Requirements 9.4, 9.6**
    - **Property 27: Leaderboard Tenant Isolation — Validates: Requirements 9.5**

- [ ] 15. Checkpoint — core services
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 16. Notification Service
  - [ ] 16.1 Implement multi-channel notification dispatch
    - Implement `QueueNotificationAsync` that routes to in-app, email (SendGrid), or SMS (Africa's Talking) based on the user's `NotificationPreferences`
    - Implement in-app notification storage and `GET /notifications/{userId}` + `PUT /notifications/{id}/read` endpoints
    - _Requirements: 15.1, 15.2, 15.4_
  - [ ] 16.2 Implement SMS retry logic with Hangfire
    - On SMS send failure, schedule a Hangfire retry job; increment `RetryCount` on each attempt; after 3 failures set `Status = Undelivered`
    - _Requirements: 15.3, 15.5_
  - [ ] 16.3 Implement inactivity alert job
    - Implement a Hangfire recurring job that queries students with `LastLoginAt < NOW() - 7 days` and queues `InactivityAlert` notifications for linked parents
    - _Requirements: 10.6_
  - [ ]* 16.4 Write property tests for Notification Service (Properties 32, 40, 41)
    - **Property 32: Inactivity Notification After 7 Days — Validates: Requirements 10.6**
    - **Property 40: Notification Routing Respects User Preferences — Validates: Requirements 15.4**
    - **Property 41: SMS Retry Logic — Validates: Requirements 15.5**

- [ ] 17. ZimBot Service
  - [ ] 17.1 Implement conversational AI chat endpoint
    - Implement `ChatAsync` using Azure OpenAI SDK / Semantic Kernel; include the student's grade level and current module context in the system prompt
    - Implement hint-only mode: detect assessment-answer requests and respond with a guiding hint instead of the direct answer
    - Implement multilingual response based on the student's `PreferredLanguage` (English, Shona, Ndebele, Kalanga)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_
  - [ ] 17.2 Implement fallback and interaction logging
    - Wrap AI provider calls with a Polly circuit breaker; return a static fallback message and log the question when the circuit is open
    - Persist every interaction to `ZimBotInteraction` records scoped to the tenant
    - Implement `GET /zimbot/logs/{tenantId}` for teacher review
    - _Requirements: 5.6, 5.7, 5.8_

- [ ] 18. Live Classroom Service
  - [ ] 18.1 Implement session scheduling and join token generation
    - Implement `ScheduleAsync` creating a `ClassroomSession` record; publish a `ClassroomScheduled` event consumed by Notification Service to send 24-hour advance notifications to students and parents
    - Implement `GetJoinTokenAsync` calling the Daily.co or Jitsi SDK to generate a participant token
    - _Requirements: 13.1, 13.2_
  - [ ] 18.2 Implement SignalR hub for presence and session control
    - Implement a SignalR hub for real-time participant presence, screen share signalling, and teacher audio/video mute controls
    - Support minimum 50 concurrent participants
    - _Requirements: 13.3, 13.4, 13.6_
  - [ ] 18.3 Implement session end, attendance, and recording
    - Implement `EndSessionAsync` that records attendance (join time + duration) for each participant and triggers recording retrieval from the video provider
    - Implement `GetRecordingUrlAsync` that returns the URL only within 30 days of `SessionEndTime`; return null/404 after expiry
    - _Requirements: 13.5, 13.6, 13.7_
  - [ ]* 18.4 Write property tests for Live Classroom Service (Properties 34, 35, 36)
    - **Property 34: Live Classroom Notification Lead Time — Validates: Requirements 13.2**
    - **Property 35: Attendance Record on Session End — Validates: Requirements 13.5**
    - **Property 36: Recording Availability Window — Validates: Requirements 13.7**

- [ ] 19. Sync Service
  - [ ] 19.1 Implement offline queue processing as a .NET Worker Service
    - Implement `ProcessOfflineQueueAsync` that reads pending `OfflineSyncQueue` records and applies them to `StudentProgress` and `AssessmentAttempt` tables
    - Implement `DetectConflictAsync` comparing local `LocalTimestamp` against server-side record timestamp
    - Implement `ResolveConflictAsync` using last-write-wins (later timestamp retained); write a `SyncConflictLog` entry for every conflict
    - _Requirements: 12.2, 12.5_
  - [ ]* 19.2 Write property test for Sync Service (Property 33)
    - **Property 33: Offline Sync Conflict Resolution by Timestamp — Validates: Requirements 12.5**

- [ ] 20. Marketplace Service
  - [ ] 20.1 Implement content pack submission and admin review gate
    - Implement `POST /marketplace/packs` that creates a pack with `Status = PendingReview`
    - Implement Platform Admin approval endpoint that transitions status to `Approved`; only `Approved` packs appear in `GET /marketplace/packs` results
    - _Requirements: 8.1, 8.2_
  - [ ] 20.2 Implement cross-tenant access request and approval
    - Implement `POST /marketplace/packs/{id}/request-access` that notifies the originating teacher
    - Implement `POST /marketplace/packs/{id}/approve-access` that grants the requesting tenant read access to only that content pack — no other originating tenant data
    - _Requirements: 8.3, 8.4_
  - [ ] 20.3 Implement attribution, ratings, and admin removal
    - Ensure all marketplace pack responses include non-empty `SchoolName` and `TeacherName` fields
    - Implement `POST /marketplace/packs/{id}/rate` for teacher ratings and reviews
    - Implement `DELETE /marketplace/packs/{id}` for Platform Admin policy violation removal with teacher notification
    - _Requirements: 8.5, 8.6, 8.7_
  - [ ]* 20.4 Write property tests for Marketplace Service (Properties 21, 22, 23)
    - **Property 21: Marketplace Submission Requires Admin Review — Validates: Requirements 8.2**
    - **Property 22: Cross-Tenant Content Access Requires Approval — Validates: Requirements 8.3, 8.4**
    - **Property 23: Marketplace Attribution Completeness — Validates: Requirements 8.5**

- [ ] 21. Student progress tracking and parent dashboard API
  - [ ] 21.1 Implement `StudentProgress` recording and module unlock logic
    - On `ModuleCompleted` event, set `IsCompleted = true` and unlock the next module in sequence
    - Calculate and store per-subject progress percentage as `(completed modules / total modules) * 100`
    - _Requirements: 4.4, 4.8_
  - [ ] 21.2 Implement parent dashboard endpoint
    - Implement `GET /parents/{parentId}/dashboard` returning `CurrentGrade`, `Subjects`, `RecentActivity`, and `OverallProgressPercent` for each linked student
    - Implement weekly summary notification job (email + SMS) for parents
    - _Requirements: 10.1, 10.2, 10.4_
  - [ ] 21.3 Implement screen time limit enforcement
    - Track daily session time per student; when `DailyScreenTimeLimitSeconds` is reached, pause the session and block new session creation until the next calendar day
    - _Requirements: 10.5_
  - [ ]* 21.4 Write property tests for progress and parent dashboard (Properties 13, 14, 29, 31)
    - **Property 13: Module Completion Unlocks Next Module — Validates: Requirements 4.4**
    - **Property 14: Progress Percentage Calculation — Validates: Requirements 4.8**
    - **Property 29: Parent Dashboard Data Completeness — Validates: Requirements 10.1**
    - **Property 31: Screen Time Limit Enforcement — Validates: Requirements 10.5**

- [ ] 22. Pre-school session management
  - [ ] 22.1 Implement 20-minute segment limit and inactivity pause
    - Track continuous interaction time per toddler session; display a rest prompt after 20 minutes of continuous interaction
    - Transition session `Status` to `Paused` after 60 seconds of no interaction events
    - _Requirements: 3.6, 3.7_
  - [ ]* 22.2 Write property tests for session management (Properties 12)
    - **Property 12: Session Inactivity Pause — Validates: Requirements 3.7**

- [ ] 23. Checkpoint — feature services
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 24. React Web PWA — foundation and authentication
  - [ ] 24.1 Scaffold React Web PWA with PWA caching and offline support
    - Create React app with Vite; configure service worker for PWA caching of module content
    - Implement offline indicator banner shown when `navigator.onLine = false`
    - _Requirements: 12.3, 12.4_
  - [ ] 24.2 Implement authentication screens and JWT management
    - Implement login, registration, email verification, and SSO redirect screens
    - Store JWT and refresh token securely; implement silent token refresh; redirect to login on token expiry without exposing session data
    - _Requirements: 1.1, 1.2, 1.6_
  - [ ] 24.3 Implement accessibility settings
    - Implement high-contrast mode toggle, font size selector (Small/Medium/Large/ExtraLarge), and text-to-speech activation button in a settings panel
    - Ensure all interactive elements are keyboard-navigable
    - _Requirements: 14.1, 14.2, 14.3, 14.5_
  - [ ]* 24.4 Write property test for font size validation (Property 37)
    - **Property 37: Font Size Preference Validation — Validates: Requirements 14.3**

- [ ] 25. React Web PWA — student learning experience
  - [ ] 25.1 Implement module viewer with content type renderers
    - Implement renderers for video (with closed captions), PDF, audio (with transcript link), and quiz content types
    - Implement 3D scene viewer with rotate/zoom/interact controls using Three.js or Babylon.js
    - _Requirements: 4.2, 4.3, 4.5, 14.4, 14.6_
  - [ ] 25.2 Implement ZimBot chat widget
    - Implement a persistent ZimBot button on all learning screens; render a chat panel with message history and language selector
    - Display fallback message when ZimBot service is unavailable
    - _Requirements: 5.1, 5.2, 5.3, 5.8_
  - [ ] 25.3 Implement student dashboard with progress display
    - Render per-subject progress percentage bars and recent activity feed
    - Display gamification points, earned badges, and leaderboard position
    - _Requirements: 4.8, 9.3, 9.4, 9.5_

- [ ] 26. React Web PWA — teacher and admin screens
  - [ ] 26.1 Implement teacher content editor and module management
    - Implement file upload UI (drag-and-drop) with client-side size validation before upload
    - Implement module builder: create/reorder content items, assign grade and subject, publish to tenant or submit to marketplace
    - _Requirements: 7.1, 7.2, 7.3, 8.1_
  - [ ] 26.2 Implement assessment builder and results dashboard
    - Implement question editor for multiple-choice, true/false, and short-answer types with optional time limit
    - Implement class results view with per-student scores, completion rates, and time-on-task table
    - _Requirements: 7.5, 7.6, 7.7_
  - [ ] 26.3 Implement school admin dashboard and tenant branding
    - Implement admin dashboard showing enrolled students, active teachers, subscription status, and storage usage
    - Implement branding settings form (logo upload, school name, primary colour picker)
    - _Requirements: 11.4, 11.5_

- [ ] 27. React Web PWA — parent dashboard and live classroom
  - [ ] 27.1 Implement parent dashboard
    - Render linked students' grade, subjects, recent activity, progress percentage, badges, and weekly summary
    - Implement screen time limit configuration control
    - _Requirements: 10.1, 10.4, 10.5_
  - [ ] 27.2 Implement live classroom join and SignalR integration
    - Implement classroom join flow using the join token from the Live Classroom Service
    - Integrate Daily.co or Jitsi embed; connect to SignalR hub for presence and teacher controls
    - _Requirements: 13.3, 13.4, 13.6_

- [ ] 28. React Native Mobile app
  - [ ] 28.1 Scaffold React Native app with offline storage
    - Create React Native project (Expo or bare workflow); configure WatermelonDB or SQLite for local offline queue storage
    - Implement offline progress queue: write `OfflineSyncQueue` records locally when offline; trigger sync on connectivity restore
    - _Requirements: 12.1, 12.2, 12.4_
  - [ ] 28.2 Implement mobile learning screens and content renderers
    - Implement video player (with captions), PDF viewer, audio player (with transcript), and quiz screens
    - Implement 3D scene viewer with touch-based rotate/zoom/interact
    - Implement ZimBot chat widget with language selector
    - _Requirements: 4.3, 4.5, 5.1, 14.4_
  - [ ] 28.3 Implement switch access navigation and accessibility
    - Ensure all interactive elements support switch access on iOS and Android
    - Implement text-to-speech activation and high-contrast mode
    - _Requirements: 14.1, 14.2, 14.5_
  - [ ] 28.4 Implement pre-school tier experience on mobile
    - Implement toddler home screen with animated character, audio pronunciation on tap, and celebratory animation on activity completion
    - Implement 20-minute segment rest prompt and 60-second inactivity pause
    - Implement language selector (English, Shona, Ndebele) for pre-school interface
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.6, 3.7, 3.8_

- [ ] 29. Checkpoint — frontends
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 30. Integration wiring — event consumers and cross-service flows
  - [ ] 30.1 Wire `ModuleCompleted` event flow
    - Ensure `ModuleCompleted` published by Content/Progress service is consumed by: Adaptive Learning (profile update), Gamification (points + badge check), Notification (parent dashboard update), and Sync Service
    - _Requirements: 4.4, 6.1, 9.3, 10.2_
  - [ ] 30.2 Wire `AssessmentSubmitted` event flow
    - Ensure `AssessmentSubmitted` is consumed by: Adaptive Learning (path update), Gamification (points + badge check), Notification (parent notification)
    - _Requirements: 6.1, 9.2, 9.3_
  - [ ] 30.3 Wire `BadgeAwarded` event flow
    - Ensure `BadgeAwarded` triggers: certificate generation job, parent in-app notification, and email notification
    - _Requirements: 9.6, 10.2_
  - [ ] 30.4 Wire `PaymentSucceeded` and `PaymentFailed` event flows
    - Ensure payment events from Billing Service trigger tenant status updates and subscriber notifications within 60 seconds
    - _Requirements: 2.3, 2.5_
  - [ ] 30.5 Wire offline sync trigger on connectivity restore
    - Implement connectivity listener in React Native and PWA that calls the Sync Service endpoint when `navigator.onLine` transitions to `true`
    - _Requirements: 12.2_

- [ ] 31. FsCheck custom arbitraries and test project setup
  - Create `EduZim.Tests/Properties/Arbitraries/EduZimArbitraries.cs` with FsCheck generators for `ApplicationUser`, `Tenant`, `AssessmentAttempt`, `Subscription`, `ContentItem`, `Notification`, and `OfflineProgressItem`
  - Configure all property test classes with `[Property(MaxTest = 500)]` for security-critical properties (P1, P2, P3, P4, P42, P44) and `[Property(MaxTest = 100)]` for others
  - _Requirements: all_

- [ ] 32. Integration tests with Testcontainers
  - [ ]* 32.1 Write integration tests for RLS policy enforcement
    - Use `WebApplicationFactory<Program>` + Testcontainers PostgreSQL to verify that queries from tenant A never return rows belonging to tenant B
    - _Requirements: 11.1, 11.3_
  - [ ]* 32.2 Write integration tests for billing webhook idempotency
    - Verify that replaying the same Stripe `payment_intent.succeeded` event twice results in exactly one `Invoice` record and one subscription status transition
    - _Requirements: 2.3, 2.7_
  - [ ]* 32.3 Write integration tests for Hangfire job execution
    - Verify soft-delete cleanup job, renewal reminder job, and inactivity alert job execute correctly against a real PostgreSQL instance
    - _Requirements: 7.4, 2.4, 10.6_
  - [ ]* 32.4 Write integration tests for offline sync conflict resolution
    - Simulate concurrent local and server progress records and verify last-write-wins resolution and conflict log creation
    - _Requirements: 12.5_

- [ ] 33. Security hardening
  - [ ] 33.1 Enforce TLS and security headers
    - Configure HTTPS redirection and HSTS in all service `Program.cs` files
    - Add security headers middleware (X-Content-Type-Options, X-Frame-Options, CSP) to the YARP gateway
    - _Requirements: 16.1_
  - [ ] 33.2 Implement Polly resilience policies for all external calls
    - Add retry + circuit breaker Polly policies to: AI service (ZimBot), SMS gateway, payment provider (Stripe/Paynow), and video provider (Daily.co/Jitsi) HTTP clients
    - _Requirements: 5.8, 15.5_

- [ ] 34. Final checkpoint — full system
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP
- Each task references specific requirements for traceability
- Checkpoints at tasks 8, 15, 23, 29, and 34 ensure incremental validation
- Property tests use FsCheck with `MaxTest = 500` for security-critical properties (P1–P4, P42, P44) and `MaxTest = 100` for all others
- All property tests must include the tag comment: `// Feature: elearning-app-zimbabwe, Property {N}: {property_text}`
- Integration tests use Testcontainers to spin up real PostgreSQL and verify RLS policies with actual database connections
