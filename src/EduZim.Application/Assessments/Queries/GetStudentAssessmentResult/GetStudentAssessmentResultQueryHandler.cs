using EduZim.Application.Assessments.DTOs;
using EduZim.Application.Assessments.Services;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Assessments.Queries.GetStudentAssessmentResult;

public sealed class GetStudentAssessmentResultQueryHandler
    : IRequestHandler<GetStudentAssessmentResultQuery, SubmitAssessmentResultDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<GetStudentAssessmentResultQueryHandler> _logger;

    public GetStudentAssessmentResultQueryHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<GetStudentAssessmentResultQueryHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<SubmitAssessmentResultDto> Handle(GetStudentAssessmentResultQuery request, CancellationToken ct)
    {
        EnsureCanViewResult(request.TenantId, request.StudentId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        await EnsureAssessmentExistsAsync(request, ct).ConfigureAwait(false);
        List<Question> questions = await LoadQuestionsAsync(request, ct).ConfigureAwait(false);
        AssessmentAttempt attempt = await LoadLatestSubmittedAttemptAsync(request, ct).ConfigureAwait(false);
        IReadOnlyList<QuestionFeedbackDto> feedback = await BuildFeedbackAsync(request, attempt, questions, ct)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "Loaded assessment result for student {StudentId}, attempt {AttemptId}.",
            request.StudentId,
            attempt.Id);

        return new SubmitAssessmentResultDto(
            attempt.Id,
            attempt.ScorePercent,
            attempt.TimeTakenSeconds,
            attempt.SubmittedAt!.Value,
            feedback);
    }

    private async Task EnsureAssessmentExistsAsync(GetStudentAssessmentResultQuery request, CancellationToken ct)
    {
        bool exists = await _db.Assessments
            .AsNoTracking()
            .AnyAsync(a => a.Id == request.AssessmentId && a.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(Assessment), request.AssessmentId);
    }

    private async Task<List<Question>> LoadQuestionsAsync(GetStudentAssessmentResultQuery request, CancellationToken ct)
    {
        return await _db.Questions
            .AsNoTracking()
            .Where(q => q.AssessmentId == request.AssessmentId && q.TenantId == request.TenantId)
            .OrderBy(q => q.Id)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task<AssessmentAttempt> LoadLatestSubmittedAttemptAsync(
        GetStudentAssessmentResultQuery request,
        CancellationToken ct)
    {
        AssessmentAttempt? attempt = await _db.AssessmentAttempts
            .AsNoTracking()
            .Where(
                a => a.AssessmentId == request.AssessmentId
                    && a.StudentId == request.StudentId
                    && a.TenantId == request.TenantId
                    && a.SubmittedAt != null)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (attempt is null)
            throw new NotFoundException("Submitted assessment result", request.StudentId);

        return attempt;
    }

    private async Task<IReadOnlyList<QuestionFeedbackDto>> BuildFeedbackAsync(
        GetStudentAssessmentResultQuery request,
        AssessmentAttempt attempt,
        List<Question> questions,
        CancellationToken ct)
    {
        List<AnswerRecord> answerRows = await _db.AnswerRecords
            .AsNoTracking()
            .Where(r => r.AssessmentAttemptId == attempt.Id && r.TenantId == request.TenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        Dictionary<Guid, string?> answersByQuestionId = answerRows.ToDictionary(r => r.QuestionId, r => r.Answer);
        var (_, feedback) = AssessmentGrading.Compute(questions, answersByQuestionId);
        return feedback;
    }

    private void EnsureCanViewResult(Guid tenantId, Guid studentId)
    {
        if (_currentUser.Role == UserRole.Student)
        {
            if (_currentUser.UserId != studentId)
            {
                throw new TenantAccessViolationException(
                    "Students may only view their own assessment results.",
                    tenantId,
                    studentId);
            }

            TenantAccessHelper.EnsureCanAccessTenantScope(_currentUser, tenantId);
            return;
        }

        TenantAccessHelper.EnsureCanViewTenantDashboard(_currentUser, tenantId);
    }
}
