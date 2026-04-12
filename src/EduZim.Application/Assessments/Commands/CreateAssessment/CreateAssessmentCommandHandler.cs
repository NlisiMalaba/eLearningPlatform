using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.Tenants;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Assessments.Commands.CreateAssessment;

public sealed class CreateAssessmentCommandHandler : IRequestHandler<CreateAssessmentCommand, Guid>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateAssessmentCommandHandler> _logger;

    public CreateAssessmentCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        ILogger<CreateAssessmentCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateAssessmentCommand request, CancellationToken ct)
    {
        TenantAccessHelper.EnsureCanManageSchoolContent(_currentUser, request.TenantId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        bool moduleExists = await _db.Modules
            .AnyAsync(m => m.Id == request.ModuleId && m.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
        if (!moduleExists)
            throw new NotFoundException(nameof(Module), request.ModuleId);

        DateTime now = DateTime.UtcNow;
        Guid assessmentId = Guid.NewGuid();
        var assessment = new Assessment
        {
            Id = assessmentId,
            TenantId = request.TenantId,
            ModuleId = request.ModuleId,
            Title = request.Title,
            TimeLimitSeconds = request.TimeLimitSeconds,
            PassingScorePercent = request.PassingScorePercent,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _db.Assessments.AddAsync(assessment, ct).ConfigureAwait(false);

        foreach (CreateAssessmentQuestionItem item in request.Questions)
        {
            Guid questionId = Guid.NewGuid();
            var question = new Question
            {
                Id = questionId,
                TenantId = request.TenantId,
                AssessmentId = assessmentId,
                Type = item.Type,
                Text = item.Text,
                Points = item.Points,
            };

            switch (item.Type)
            {
                case QuestionType.MultipleChoice:
                    AddMultipleChoiceOptions(request.TenantId, question, item);
                    break;
                case QuestionType.TrueFalse:
                    AddTrueFalseOptions(request.TenantId, question, item);
                    break;
                case QuestionType.ShortAnswer:
                    question.CorrectAnswer = item.CorrectShortAnswer!.Trim();
                    break;
                default:
                    throw new DomainException($"Unsupported question type {item.Type}.");
            }

            await _db.Questions.AddAsync(question, ct).ConfigureAwait(false);
        }

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Created assessment {AssessmentId} for module {ModuleId} in tenant {TenantId}.",
            assessmentId,
            request.ModuleId,
            request.TenantId);

        return assessmentId;
    }

    private static void AddMultipleChoiceOptions(Guid tenantId, Question question, CreateAssessmentQuestionItem item)
    {
        var optionIds = new List<Guid>();
        for (int i = 0; i < item.OptionTexts!.Count; i++)
        {
            Guid oid = Guid.NewGuid();
            optionIds.Add(oid);
            question.Options.Add(
                new QuestionOption
                {
                    Id = oid,
                    TenantId = tenantId,
                    QuestionId = question.Id,
                    Text = item.OptionTexts[i],
                    OrderIndex = i,
                });
        }

        question.CorrectAnswer = optionIds[item.CorrectOptionIndex!.Value].ToString();
    }

    private static void AddTrueFalseOptions(Guid tenantId, Question question, CreateAssessmentQuestionItem item)
    {
        string[] texts = item.OptionTexts is { Count: >= 2 }
            ? [item.OptionTexts[0], item.OptionTexts[1]]
            : ["True", "False"];
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();
        question.Options.Add(
            new QuestionOption
            {
                Id = firstId,
                TenantId = tenantId,
                QuestionId = question.Id,
                Text = texts[0],
                OrderIndex = 0,
            });
        question.Options.Add(
            new QuestionOption
            {
                Id = secondId,
                TenantId = tenantId,
                QuestionId = question.Id,
                Text = texts[1],
                OrderIndex = 1,
            });
        question.CorrectAnswer = item.CorrectOptionIndex == 0 ? firstId.ToString() : secondId.ToString();
    }
}
