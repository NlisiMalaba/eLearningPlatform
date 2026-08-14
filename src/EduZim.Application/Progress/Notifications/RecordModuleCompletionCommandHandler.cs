using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Progress.Notifications;

/// <summary>Records module completion, unlocks the next module, and stores per-subject progress.</summary>
public sealed class RecordModuleCompletionCommandHandler : INotificationHandler<ModuleCompletedNotification>
{
    private readonly IEduZimDbContext _db;
    private readonly ILogger<RecordModuleCompletionCommandHandler> _logger;

    public RecordModuleCompletionCommandHandler(
        IEduZimDbContext db,
        ILogger<RecordModuleCompletionCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(ModuleCompletedNotification notification, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(notification.TenantId, ct).ConfigureAwait(false);

        Module? module = await LoadModuleAsync(notification, ct).ConfigureAwait(false);
        if (module is null)
        {
            _logger.LogWarning(
                "Module completion ignored; module {ModuleId} was not found in tenant {TenantId}.",
                notification.ModuleId,
                notification.TenantId);
            return;
        }

        await ApplyCompletionAsync(notification, module, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Recorded module completion for student {StudentId} module {ModuleId}.",
            notification.StudentId,
            notification.ModuleId);
    }

    private async Task ApplyCompletionAsync(
        ModuleCompletedNotification notification,
        Module module,
        CancellationToken ct)
    {
        List<Module> modules = await LoadTenantModulesAsync(notification.TenantId, ct).ConfigureAwait(false);
        List<StudentProgress> progresses = await LoadStudentProgressAsync(notification, ct).ConfigureAwait(false);
        DateTime utcNow = DateTime.UtcNow;

        await MarkCompletedAsync(notification, module, progresses, utcNow, ct).ConfigureAwait(false);
        await UnlockNextAsync(notification, module, modules, progresses, utcNow, ct).ConfigureAwait(false);
        await StoreSubjectPercentAsync(notification, module.Subject, modules, progresses, utcNow, ct)
            .ConfigureAwait(false);
    }

    private async Task MarkCompletedAsync(
        ModuleCompletedNotification notification,
        Module module,
        List<StudentProgress> progresses,
        DateTime utcNow,
        CancellationToken ct)
    {
        StudentProgress? existing = progresses.FirstOrDefault(p => p.ModuleId == module.Id);
        if (existing is not null)
        {
            existing.IsCompleted = true;
            existing.IsUnlocked = true;
            existing.CompletedAt ??= utcNow;
            existing.UpdatedAt = utcNow;
            return;
        }

        StudentProgress created = ModuleCompletionFactory.Progress(
            notification,
            module.Id,
            utcNow,
            completed: true,
            unlocked: true);
        progresses.Add(created);
        await _db.StudentProgresses.AddAsync(created, ct).ConfigureAwait(false);
    }

    private async Task UnlockNextAsync(
        ModuleCompletedNotification notification,
        Module module,
        List<Module> modules,
        List<StudentProgress> progresses,
        DateTime utcNow,
        CancellationToken ct)
    {
        List<ModuleUnlockRules.ModuleSequenceRow> rows = modules
            .Select(m => new ModuleUnlockRules.ModuleSequenceRow(m.Id, m.Subject, m.Grade, m.SequenceOrder))
            .ToList();
        Guid? nextId = ModuleUnlockRules.NextModuleId(rows, module.Id);
        if (nextId is not Guid nextModuleId)
            return;

        StudentProgress? next = progresses.FirstOrDefault(p => p.ModuleId == nextModuleId);
        if (next is not null)
        {
            next.IsUnlocked = true;
            next.UpdatedAt = utcNow;
            return;
        }

        StudentProgress created = ModuleCompletionFactory.Progress(
            notification,
            nextModuleId,
            utcNow,
            completed: false,
            unlocked: true);
        progresses.Add(created);
        await _db.StudentProgresses.AddAsync(created, ct).ConfigureAwait(false);
    }

    private async Task StoreSubjectPercentAsync(
        ModuleCompletedNotification notification,
        string subject,
        List<Module> modules,
        List<StudentProgress> progresses,
        DateTime utcNow,
        CancellationToken ct)
    {
        HashSet<Guid> completedIds = progresses.Where(p => p.IsCompleted).Select(p => p.ModuleId).ToHashSet();
        List<ProgressPercentageRules.ModuleSubjectRow> rows = modules
            .Select(m => new ProgressPercentageRules.ModuleSubjectRow(m.Id, m.Subject))
            .ToList();
        int percent = ProgressPercentageRules.PerSubject(rows, completedIds)
            .FirstOrDefault(s => s.Subject == subject)
            .ProgressPercent;

        SubjectProgress? stored = await _db.SubjectProgresses
            .FirstOrDefaultAsync(
                s => s.TenantId == notification.TenantId
                    && s.StudentId == notification.StudentId
                    && s.Subject == subject,
                ct)
            .ConfigureAwait(false);

        if (stored is not null)
        {
            stored.ProgressPercent = percent;
            stored.UpdatedAt = utcNow;
            return;
        }

        await _db.SubjectProgresses
            .AddAsync(ModuleCompletionFactory.Subject(notification, subject, percent, utcNow), ct)
            .ConfigureAwait(false);
    }

    private async Task<Module?> LoadModuleAsync(ModuleCompletedNotification notification, CancellationToken ct)
    {
        return await _db.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.Id == notification.ModuleId && m.TenantId == notification.TenantId,
                ct)
            .ConfigureAwait(false);
    }

    private async Task<List<Module>> LoadTenantModulesAsync(Guid tenantId, CancellationToken ct)
    {
        return await _db.Modules
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<List<StudentProgress>> LoadStudentProgressAsync(
        ModuleCompletedNotification notification,
        CancellationToken ct)
    {
        return await _db.StudentProgresses
            .Where(p => p.TenantId == notification.TenantId && p.StudentId == notification.StudentId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
