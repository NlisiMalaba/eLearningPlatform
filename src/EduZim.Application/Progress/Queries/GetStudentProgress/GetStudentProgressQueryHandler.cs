using EduZim.Application.Common.Interfaces;
using EduZim.Application.Progress.DTOs;
using EduZim.Application.Progress.Services;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Progress.Queries.GetStudentProgress;

public sealed class GetStudentProgressQueryHandler : IRequestHandler<GetStudentProgressQuery, StudentProgressDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetStudentProgressQueryHandler> _logger;

    public GetStudentProgressQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetStudentProgressQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<StudentProgressDto> Handle(GetStudentProgressQuery request, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);
        await ProgressStudentAccess
            .EnsureCanViewAsync(_db, _currentUser, request.TenantId, request.StudentId, ct)
            .ConfigureAwait(false);

        List<Module> modules = await LoadModulesAsync(request.TenantId, ct).ConfigureAwait(false);
        List<StudentProgress> progresses = await LoadProgressAsync(request, ct).ConfigureAwait(false);
        StudentProgressDto dto = Map(request.StudentId, modules, progresses);

        _logger.LogDebug(
            "Student progress queried for student {StudentId}: {SubjectCount} subject(s).",
            request.StudentId,
            dto.Subjects.Count);
        return dto;
    }

    private async Task<List<Module>> LoadModulesAsync(Guid tenantId, CancellationToken ct)
    {
        return await _db.Modules
            .AsNoTracking()
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<List<StudentProgress>> LoadProgressAsync(GetStudentProgressQuery request, CancellationToken ct)
    {
        return await _db.StudentProgresses
            .AsNoTracking()
            .Where(p => p.TenantId == request.TenantId && p.StudentId == request.StudentId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private static StudentProgressDto Map(
        Guid studentId,
        List<Module> modules,
        List<StudentProgress> progresses)
    {
        HashSet<Guid> completedIds = progresses.Where(p => p.IsCompleted).Select(p => p.ModuleId).ToHashSet();
        HashSet<Guid> unlockedIds = progresses.Where(p => p.IsUnlocked).Select(p => p.ModuleId).ToHashSet();
        List<ModuleUnlockRules.ModuleSequenceRow> sequence = modules
            .Select(m => new ModuleUnlockRules.ModuleSequenceRow(m.Id, m.Subject, m.Grade, m.SequenceOrder))
            .ToList();
        Dictionary<Guid, StudentProgress> byModule = progresses.ToDictionary(p => p.ModuleId);

        IReadOnlyList<SubjectProgressDto> subjects = modules
            .GroupBy(m => m.Subject, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => MapSubject(g.Key, g.ToList(), sequence, completedIds, unlockedIds, byModule))
            .ToList();

        return new StudentProgressDto(studentId, subjects);
    }

    private static SubjectProgressDto MapSubject(
        string subject,
        List<Module> subjectModules,
        List<ModuleUnlockRules.ModuleSequenceRow> sequence,
        HashSet<Guid> completedIds,
        HashSet<Guid> unlockedIds,
        Dictionary<Guid, StudentProgress> byModule)
    {
        int completed = subjectModules.Count(m => completedIds.Contains(m.Id));
        int percent = ProgressPercentageRules.Calculate(completed, subjectModules.Count);
        IReadOnlyList<ModuleProgressDto> moduleDtos = subjectModules
            .OrderBy(m => m.Grade)
            .ThenBy(m => m.SequenceOrder)
            .ThenBy(m => m.Id)
            .Select(m => MapModule(m, sequence, completedIds, unlockedIds, byModule))
            .ToList();
        return new SubjectProgressDto(subject, percent, moduleDtos);
    }

    private static ModuleProgressDto MapModule(
        Module module,
        List<ModuleUnlockRules.ModuleSequenceRow> sequence,
        HashSet<Guid> completedIds,
        HashSet<Guid> unlockedIds,
        Dictionary<Guid, StudentProgress> byModule)
    {
        byModule.TryGetValue(module.Id, out StudentProgress? progress);
        bool accessible = ModuleUnlockRules.IsAccessible(sequence, completedIds, unlockedIds, module.Id);
        return new ModuleProgressDto(
            module.Id,
            module.Title,
            module.Grade,
            module.SequenceOrder,
            progress?.IsCompleted ?? false,
            accessible,
            progress?.CompletedAt);
    }
}
