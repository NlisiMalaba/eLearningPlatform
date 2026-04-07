# Requirements Document

## Introduction

**EduZim** is a Zimbabwe-focused eLearning platform designed to make learning engaging, accessible, and effective for children from toddler age through Grade 7. The platform leverages modern technology — including 3D environments, animations, AI-powered tutoring, voice/audio, videos, games, and collaborative tools — to deliver curriculum-aligned education that meets Zimbabwean standards.

EduZim serves two primary audiences:

1. **Pre-school (ECD/Toddlers, ages 2–5)**: A parent-subscribed consumer tier where young children learn foundational concepts (alphabet, numbers, shapes, colours, basic language) through immersive, play-based AI and multimedia experiences — think a modern, interactive Encarta Kids built for Zimbabwe.

2. **School tier (ECD Grade 0 – Grade 7)**: A multi-tenant B2B tier where schools subscribe and each school operates in its own isolated environment. Teachers create and manage content, students learn and are assessed, and parents/guardians track progress — all aligned to the Zimbabwe Ministry of Primary and Secondary Education (MoPSE) curriculum.

### Architecture Decision: Clean Architecture Monolith

EduZim is implemented as a **single deployable ASP.NET Core (.NET 10+) application** structured using Clean Architecture principles. The solution is organised into four layers with strict dependency rules:

1. **Domain** (`EduZim.Domain`) — Entities, value objects, domain events, enums, and domain interfaces. No dependencies on any external framework or infrastructure concern.
2. **Application** (`EduZim.Application`) — Use cases implemented as MediatR command and query handlers, organised by feature folder. Contains DTOs, FluentValidation validators, and interfaces for infrastructure services. Depends only on Domain.
3. **Infrastructure** (`EduZim.Infrastructure`) — EF Core `EduZimDbContext`, repository implementations, external service integrations (AI, SMS, email, payment, video), Hangfire background jobs, S3 storage, Redis cache, and SignalR hubs. Depends on Application and Domain.
4. **Presentation** (`EduZim.API`) — ASP.NET Core minimal API endpoints, middleware, JWT authentication, and request/response mapping. Depends only on Application.

This monolithic approach eliminates the operational complexity of distributed services while retaining clean separation of concerns through layer boundaries enforced by project references.

### Multi-Tenancy Strategy

The school tier is **multi-tenant with strict data isolation**. Each school is a separate tenant with its own data boundary — students, teachers, content, and results from one school are never visible to another school by default. Content sharing between schools is opt-in and request-based. A single **PostgreSQL database** with Row-Level Security (RLS) enforces tenant isolation at the data layer; the application layer validates tenant claims on every request as a defence-in-depth measure.

The pre-school consumer tier is a **single shared platform** — parents subscribe individually and children access a common content library.

### Innovative Feature Additions

Beyond the core request, the following innovations are proposed:

- **AI Tutor ("ZimBot")**: A conversational AI tutor that speaks Shona, Ndebele, and English, adapts to each child's learning pace, and provides hints rather than direct answers to encourage critical thinking.
- **Offline Mode**: Given Zimbabwe's connectivity challenges, all core content is downloadable for offline use, with progress syncing when connectivity is restored.
- **Adaptive Learning Paths**: AI analyses each student's performance and dynamically adjusts difficulty and content recommendations.
- **Parent Dashboard**: Real-time visibility into a child's progress, time spent, achievements, and areas needing attention.
- **Gamification Engine**: Points, badges, leaderboards (within a school), and milestone rewards to drive engagement.
- **Cross-School Content Marketplace**: Teachers can publish content packs; other schools can request access, fostering a collaborative educator community.
- **Accessibility Features**: Text-to-speech, adjustable font sizes, high-contrast mode, and support for learners with visual or hearing impairments.
- **Live Virtual Classrooms**: Scheduled video sessions between teachers and students within a school's tenant.
- **Progress Certificates**: Auto-generated, printable certificates for completed modules and achievements.
- **SMS/USSD Notifications**: Lightweight notifications for parents in low-connectivity areas via SMS.

---

## Glossary

- **EduZim**: The eLearning platform described in this document.
- **Platform**: The EduZim system as a whole, including all tiers and use cases.
- **Pre-school Tier**: The consumer-facing product for toddlers aged 2–5, subscribed to by parents.
- **School Tier**: The multi-tenant B2B product for ECD Grade 0 – Grade 7 students enrolled in subscribing schools.
- **Tenant**: A single school and all its associated data, users, and content within the School Tier.
- **Student**: A learner enrolled in the Platform, either in the Pre-school Tier or the School Tier.
- **Teacher**: An educator within a Tenant who creates content and manages students.
- **Parent/Guardian**: An adult responsible for a Student, with read-only visibility into that Student's progress.
- **School Admin**: The administrator of a Tenant, responsible for managing teachers, students, and school-level settings.
- **Platform Admin**: EduZim staff with access to platform-wide configuration and tenant management.
- **Content**: Any learning material including videos, PDFs, audio, 3D scenes, animations, quizzes, and games.
- **Module**: A structured collection of Content items organised around a learning objective.
- **Curriculum**: The Zimbabwe MoPSE-aligned syllabus for ECD Grade 0 – Grade 7.
- **ZimBot**: The AI-powered conversational tutor embedded in the Platform.
- **Gamification Engine**: The application use cases responsible for points, badges, leaderboards, and rewards.
- **Subscription**: A recurring payment plan (monthly, termly, or yearly) granting access to the Platform.
- **Offline Mode**: The ability to access downloaded Content without an active internet connection.
- **Content Marketplace**: The feature allowing Teachers to publish and share Content packs across Tenants.
- **Live Classroom**: A scheduled, real-time video session between a Teacher and Students within a Tenant.
- **Assessment**: A quiz, test, or exercise used to evaluate a Student's understanding.
- **Progress Report**: A summary of a Student's performance, engagement, and achievements over a period.
- **SMS_Service**: The external SMS gateway used to deliver notifications to Parents in low-connectivity areas.
- **Sync Use Case**: The application use case that reconciles offline progress data with the server when connectivity is restored.

---

## Requirements

### Requirement 1: User Registration and Authentication

**User Story:** As a parent, school admin, teacher, or student, I want to register and securely log in to EduZim, so that I can access the features relevant to my role.

#### Acceptance Criteria

1. THE Platform SHALL support the following roles: Student, Teacher, Parent/Guardian, School Admin, and Platform Admin.
2. WHEN a parent registers for the Pre-school Tier, THE Platform SHALL collect a name, email address, and password, and send an email verification link before activating the account.
3. WHEN a School Admin registers a new Tenant, THE Platform SHALL create an isolated data environment for that school before granting access.
4. WHEN a Teacher or Student account is created within a Tenant, THE School Admin SHALL assign the account to that Tenant, and THE Platform SHALL prevent that account from accessing any other Tenant's data.
5. WHEN a user submits incorrect credentials 5 consecutive times, THE Platform SHALL lock the account for 15 minutes and notify the account owner via email.
6. IF a user's session token expires, THEN THE Platform SHALL redirect the user to the login screen without exposing session data.
7. WHERE single sign-on (SSO) is configured for a Tenant, THE Platform SHALL authenticate users via the Tenant's identity provider.
8. THE Platform SHALL store all passwords using a cryptographic hashing algorithm with a per-user salt.

---

### Requirement 2: Subscription and Billing

**User Story:** As a parent or school administrator, I want to subscribe to EduZim on a flexible billing cycle, so that I can choose a plan that fits my budget.

#### Acceptance Criteria

1. THE Platform SHALL offer Pre-school Tier subscriptions on monthly and yearly billing cycles.
2. THE Platform SHALL offer School Tier subscriptions on monthly, termly (per Zimbabwe school term), and yearly billing cycles.
3. WHEN a Subscription payment is successfully processed, THE Platform SHALL activate or extend the subscriber's access within 60 seconds of payment confirmation.
4. WHEN a Subscription is within 7 days of expiry, THE Platform SHALL send a renewal reminder to the subscriber via email and, where a phone number is registered, via SMS.
5. IF a Subscription payment fails, THEN THE Platform SHALL notify the subscriber and provide a 7-day grace period before suspending access.
6. WHEN a Subscription is suspended, THE Platform SHALL preserve all Student progress data for a minimum of 90 days before deletion.
7. THE Platform SHALL generate a downloadable invoice for every successful payment.
8. WHERE a school subscribes for multiple students, THE Platform SHALL calculate the total fee based on the number of enrolled Students and the selected billing cycle.

---

### Requirement 3: Pre-school Learning Experience (Toddlers, Ages 2–5)

**User Story:** As a parent, I want my toddler to learn foundational concepts through engaging, age-appropriate multimedia, so that my child is school-ready and intellectually stimulated.

#### Acceptance Criteria

1. THE Platform SHALL provide Pre-school Tier Content covering: the English alphabet, numbers 1–100, basic shapes, colours, animals, body parts, simple Vernacular vocabulary.
2. WHEN a toddler interacts with a letter or number, THE Platform SHALL play an audio pronunciation and display a 3D animated character demonstrating the concept.
3. THE Platform SHALL present all Pre-school Tier Content through interactive animations, 3D scenes, songs, and short videos not exceeding 5 minutes in duration.
4. WHEN a toddler completes a learning activity, THE Platform SHALL display a celebratory animation and award a star to reinforce positive engagement.
5. THE Platform SHALL include at least one interactive game per foundational concept to reinforce learning through play.
6. WHILE a toddler session is active, THE Platform SHALL limit continuous screen interaction to 20-minute segments and display a rest prompt before allowing the session to continue.
7. IF a toddler does not interact with the screen for 60 seconds, THEN THE Platform SHALL pause the session and display a prompt to resume.
8. THE Platform SHALL allow a Parent to configure the interface language as English, Shona, or Ndebele for the Pre-school Tier.

---

### Requirement 4: School Tier Learning Experience (ECD Grade 0 – Grade 7)

**User Story:** As a student enrolled in a subscribing school, I want to access curriculum-aligned learning content through engaging multimedia, so that I can understand and retain concepts more effectively.

#### Acceptance Criteria

1. THE Platform SHALL organise School Tier Content by grade level (ECD Grade 0 through Grade 7) and subject, aligned to the Zimbabwe MoPSE Curriculum.
2. WHEN a Student selects a subject and topic, THE Platform SHALL present Content in a structured Module sequence including explanations, animations or 3D visualisations, worked examples, and an Assessment.
3. THE Platform SHALL support the following Content types within a Module: video, PDF, audio, 3D interactive scene, animation, quiz, and game.
4. WHEN a Student completes a Module, THE Platform SHALL record the completion, update the Student's progress, and unlock the next Module in the sequence.
5. WHILE a Student is viewing a 3D interactive scene, THE Platform SHALL allow the Student to rotate, zoom, and interact with the scene using touch or mouse input.
6. THE Platform SHALL provide Content in English as the primary language, with Kalanga and Ndebele translations available for selected subjects.
7. WHERE a Teacher has published supplementary Content for a Student's grade and subject, THE Platform SHALL make that Content available to the Student within the same Tenant.
8. THE Platform SHALL display a Student's overall progress percentage per subject on the Student's dashboard.

---

### Requirement 5: AI Tutor (ZimBot)

**User Story:** As a student, I want an AI tutor that understands my questions and guides me through difficult concepts, so that I can get help at any time without waiting for a teacher.

#### Acceptance Criteria

1. THE Platform SHALL embed ZimBot in all learning screens, accessible via a persistent button.
2. WHEN a Student submits a question to ZimBot, THE Platform SHALL return a contextually relevant response within 5 seconds under normal network conditions.
3. ZimBot SHALL respond in the language selected by the Student: English, Ndebele, Kalanga, etc.
4. WHEN a Student asks ZimBot for a direct answer to an Assessment question, ZimBot SHALL provide a guiding hint rather than the answer, to encourage independent thinking.
5. ZimBot SHALL adapt the complexity of its explanations based on the Student's current grade level as recorded in the Platform.
6. WHEN ZimBot cannot answer a question with sufficient confidence, THE Platform SHALL log the question and present the Student with a suggestion to ask their Teacher.
7. THE Platform SHALL allow a Teacher to review ZimBot interaction logs for Students within their Tenant to identify common areas of difficulty.
8. IF ZimBot's AI service is unavailable, THEN THE Platform SHALL display a clear message to the Student and offer access to static help resources.

---

### Requirement 6: Adaptive Learning Paths

**User Story:** As a student, I want the platform to adjust the difficulty and content recommendations based on my performance, so that I am always learning at the right level.

#### Acceptance Criteria

1. THE Platform SHALL analyse each Student's Assessment scores and time-on-task after every completed Module to update the Student's learning profile.
2. WHEN a Student scores below 60% on an Assessment, THE Platform SHALL recommend remedial Content for the relevant topic before advancing the Student to the next Module.
3. WHEN a Student scores 85% or above on an Assessment, THE Platform SHALL offer an optional advanced-level extension activity for the same topic.
4. THE Platform SHALL generate a weekly Adaptive Learning Path summary for each Student, visible to the Student's Teacher and Parent/Guardian.
5. WHILE a Student is on a remedial Content path, THE Platform SHALL adjust the difficulty of practice questions downward in increments until the Student achieves a score of 70% or above.
6. THE Platform SHALL NOT advance a Student to the next grade-level Module until the Student has completed all required Assessments for the current Module with a passing score of 60% or above.

---

### Requirement 7: Teacher Content Creation and Management

**User Story:** As a teacher, I want to create, upload, and organise my own learning content for my students, so that I can supplement the standard curriculum with materials tailored to my class.

#### Acceptance Criteria

1. THE Platform SHALL provide Teachers with a content editor supporting upload of PDF, video (MP4, max 500 MB), and audio (MP3/WAV, max 50 MB) files.
2. WHEN a Teacher publishes Content, THE Platform SHALL make it available only to Students within the same Tenant unless the Teacher explicitly submits it to the Content Marketplace.
3. THE Platform SHALL allow a Teacher to organise Content into Modules and assign Modules to specific grade levels and subjects within their Tenant.
4. WHEN a Teacher deletes a Content item, THE Platform SHALL retain the item in an archived state for 30 days before permanent deletion, allowing recovery.
5. THE Platform SHALL allow a Teacher to create Assessments consisting of multiple-choice, true/false, and short-answer question types.
6. WHEN a Teacher assigns an Assessment to a class, THE Platform SHALL notify all enrolled Students in that class via an in-app notification.
7. THE Platform SHALL provide Teachers with a dashboard showing per-student and per-class Assessment results, completion rates, and time-on-task metrics.

---

### Requirement 8: Content Marketplace and Cross-School Sharing

**User Story:** As a teacher, I want to share my content with other schools and access content created by teachers at other schools, so that students across Zimbabwe benefit from the best available materials.

#### Acceptance Criteria

1. THE Platform SHALL provide a Content Marketplace where Teachers can publish Content packs for discovery by Teachers at other Tenants.
2. WHEN a Teacher submits a Content pack to the Content Marketplace, THE Platform SHALL route the submission for review by a Platform Admin before it becomes publicly discoverable.
3. WHEN a Teacher at one Tenant requests access to a Content pack from another Tenant, THE Platform SHALL notify the originating Teacher and require explicit approval before granting access.
4. WHEN access to a shared Content pack is approved, THE Platform SHALL make the Content available within the requesting Tenant without exposing any other data from the originating Tenant.
5. THE Platform SHALL display attribution (school name and teacher name) on all Content packs in the Content Marketplace.
6. IF a Content pack is found to violate platform content policies, THEN THE Platform SHALL allow a Platform Admin to remove the pack from the Marketplace and notify the submitting Teacher.
7. THE Platform SHALL allow Teachers to rate and review Content packs in the Marketplace, with ratings visible to all Teachers.

---

### Requirement 9: Assessments, Progress Tracking, and Gamification

**User Story:** As a teacher and parent, I want students to be assessed regularly and rewarded for progress, so that learning is measurable and students stay motivated.

#### Acceptance Criteria

1. THE Platform SHALL record every Assessment attempt, including score, time taken, and date, in the Student's permanent record within the Tenant.
2. WHEN a Student completes an Assessment, THE Platform SHALL display the score and provide feedback on incorrect answers immediately after submission.
3. THE Gamification Engine SHALL award points to a Student upon completion of each Module and Assessment, with bonus points for scores above 85%.
4. THE Gamification Engine SHALL award badges for milestones including: first Module completed, 5 consecutive days of learning, subject mastery (all Modules in a subject completed), and grade completion.
5. THE Platform SHALL display a leaderboard of top-performing Students within a Tenant, visible only to Students and Teachers within that Tenant.
6. WHEN a Student earns a milestone badge, THE Platform SHALL generate a printable Progress Certificate and notify the Student's Parent/Guardian via in-app notification and email.
7. THE Platform SHALL provide Parents/Guardians with a Progress Report showing their child's scores, badges earned, time spent, and recommended focus areas, updated weekly.
8. WHEN a Teacher creates a timed Assessment, THE Platform SHALL enforce the time limit and auto-submit the Assessment when the time expires.

---

### Requirement 10: Parent/Guardian Dashboard

**User Story:** As a parent or guardian, I want to monitor my child's learning activity and progress, so that I can support their education and stay informed.

#### Acceptance Criteria

1. THE Platform SHALL provide each Parent/Guardian with a dashboard showing all linked Students' current grade, subjects, recent activity, and overall progress.
2. WHEN a Student completes a Module or earns a badge, THE Platform SHALL update the Parent/Guardian dashboard within 5 minutes.
3. THE Platform SHALL allow a Parent/Guardian to link to a Student account using a unique invite code generated by the School Admin (School Tier) or during registration (Pre-school Tier).
4. THE Platform SHALL send a weekly summary notification to Parents/Guardians via email and, where a phone number is registered, via SMS, summarising the Student's activity for the week.
5. THE Platform SHALL allow a Parent/Guardian to set daily screen time limits for their child's account, and THE Platform SHALL enforce those limits by pausing the session when the limit is reached.
6. IF a Student has not logged in for 7 consecutive days, THEN THE Platform SHALL notify the linked Parent/Guardian via email and SMS.

---

### Requirement 11: Multi-Tenancy and Data Isolation

**User Story:** As a school administrator, I want my school's data to be completely isolated from other schools, so that student privacy and institutional confidentiality are maintained.

#### Acceptance Criteria

1. THE Platform SHALL ensure that no Student, Teacher, or Content data from one Tenant is accessible to users of any other Tenant through any interface or API endpoint.
2. WHEN a School Admin creates a Tenant, THE Platform SHALL provision a logically isolated data partition for that Tenant before any users are onboarded.
3. THE Platform SHALL enforce Tenant-scoped access control on every API request, rejecting requests where the authenticated user's Tenant does not match the requested resource's Tenant.
4. THE Platform SHALL allow a School Admin to customise the Tenant's branding (school logo, school name, and colour scheme) within the Platform interface.
5. THE Platform SHALL provide a School Admin with a management dashboard showing total enrolled Students, active Teachers, Subscription status, and storage usage.
6. WHEN a Tenant's Subscription expires, THE Platform SHALL restrict access for all users of that Tenant while preserving all data for 90 days.
7. THE Platform SHALL support a minimum of 500 concurrent Tenants without degradation of response times beyond 2 seconds for standard page loads.

---

### Requirement 12: Offline Mode

**User Story:** As a student in an area with unreliable internet, I want to download content and continue learning offline, so that connectivity issues do not interrupt my education.

#### Acceptance Criteria

1. THE Platform SHALL allow Students and Parents to download Modules for offline access from within the app.
2. WHEN a Student completes a Module or Assessment in Offline Mode, THE Sync Use Case SHALL queue the progress data locally and upload it to the server within 60 seconds of internet connectivity being restored.
3. WHILE a Student is in Offline Mode, THE Platform SHALL display a clear indicator showing that the session is offline and that progress will sync when connectivity is restored.
4. THE Platform SHALL support offline access for downloaded Content on Android and iOS mobile devices and on web browsers that support Progressive Web App (PWA) caching.
5. IF a conflict is detected between locally queued progress and server-side progress during sync, THEN THE Sync Use Case SHALL retain the record with the later timestamp and log the conflict for review.
6. THE Platform SHALL allow a School Admin to configure which Modules are pre-downloaded to student devices during initial app setup within their Tenant.

---

### Requirement 13: Live Virtual Classrooms

**User Story:** As a teacher, I want to conduct live video sessions with my students within the platform, so that I can deliver real-time instruction without leaving the EduZim environment.

#### Acceptance Criteria

1. THE Platform SHALL allow a Teacher to schedule a Live Classroom session, specifying date, time, duration, and the class or group of Students to invite.
2. WHEN a Live Classroom session is scheduled, THE Platform SHALL send an in-app notification and email to all invited Students and their linked Parents/Guardians at least 24 hours before the session.
3. WHEN a Live Classroom session starts, THE Platform SHALL allow the Teacher to share their screen, present Content from the Platform, and enable or disable Student audio and video.
4. THE Platform SHALL support a minimum of 50 concurrent participants in a single Live Classroom session.
5. WHEN a Live Classroom session ends, THE Platform SHALL generate an attendance record showing which Students joined and the duration of their participation.
6. IF a Student's internet connection drops during a Live Classroom session, THEN THE Platform SHALL allow the Student to rejoin the session without requiring Teacher intervention.
7. THE Platform SHALL record Live Classroom sessions and make the recording available to enrolled Students within the Tenant for 30 days after the session.

---

### Requirement 14: Accessibility

**User Story:** As a student or toddler with a visual or hearing impairment, I want the platform to accommodate my needs, so that I can learn without barriers.

#### Acceptance Criteria

1. THE Platform SHALL provide a text-to-speech feature that reads on-screen text aloud in English, Shona, or Ndebele upon user activation.
2. THE Platform SHALL provide a high-contrast display mode togglable from the settings menu.
3. THE Platform SHALL allow users to adjust the base font size to small, medium, large, or extra-large from the settings menu.
4. WHEN a video or animation contains spoken audio, THE Platform SHALL provide closed captions in the language of the Content.
5. THE Platform SHALL ensure all interactive elements are navigable via keyboard on web and via switch access on mobile devices.
6. WHERE audio-only Content is provided, THE Platform SHALL include a written transcript accessible from the same screen.

---

### Requirement 15: Notifications and Communication

**User Story:** As a parent, teacher, or student, I want to receive timely notifications about important events, so that I stay informed and engaged with the platform.

#### Acceptance Criteria

1. THE Platform SHALL deliver in-app notifications for: new Content assigned, Assessment due dates, badge awards, Live Classroom reminders, and Subscription renewal alerts.
2. WHEN a notification is generated, THE Platform SHALL deliver it via in-app notification within 30 seconds and via email within 5 minutes.
3. WHERE a user has registered a mobile phone number, THE SMS_Service SHALL deliver critical notifications (Subscription expiry, session reminders) via SMS within 10 minutes.
4. THE Platform SHALL allow each user to configure notification preferences, selecting which notification types are delivered via in-app, email, or SMS channels.
5. IF the SMS_Service is unavailable, THEN THE Platform SHALL log the failed SMS and retry delivery up to 3 times at 10-minute intervals before marking the notification as undelivered.

---

### Requirement 16: Security and Data Privacy

**User Story:** As a school administrator and parent, I want student data to be protected and handled in compliance with applicable privacy standards, so that children's information is safe.

#### Acceptance Criteria

1. THE Platform SHALL encrypt all data in transit using TLS 1.2 or higher.
2. THE Platform SHALL encrypt all personally identifiable information (PII) at rest using AES-256 encryption.
3. THE Platform SHALL comply with the Zimbabwe Data Protection Act and, where applicable, GDPR principles for the handling of children's data.
4. WHEN a School Admin or Parent/Guardian requests deletion of a Student's data, THE Platform SHALL permanently delete all associated PII within 30 days and confirm deletion via email.
5. THE Platform SHALL conduct and log access control checks on every API request, and THE Platform SHALL reject any request that fails authorisation with an HTTP 403 response.
6. THE Platform SHALL retain audit logs of all administrative actions (user creation, deletion, content approval, Subscription changes) for a minimum of 12 months.
7. WHEN a Platform Admin exports data for a Tenant, THE Platform SHALL require two-factor authentication confirmation before initiating the export.
