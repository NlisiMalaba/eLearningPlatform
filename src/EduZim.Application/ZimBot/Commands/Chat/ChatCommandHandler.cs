using EduZim.Application.Common.Interfaces;
using EduZim.Application.Exceptions;
using EduZim.Application.ZimBot.DTOs;
using EduZim.Application.ZimBot.Services;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.ZimBot.Commands.Chat;

public sealed class ChatCommandHandler : IRequestHandler<ChatCommand, ZimBotChatDto>
{
    private readonly IEduZimDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAiService _ai;
    private readonly IPublisher _publisher;
    private readonly ILogger<ChatCommandHandler> _logger;

    public ChatCommandHandler(
        IEduZimDbContext db,
        ICurrentUser currentUser,
        IAiService ai,
        IPublisher publisher,
        ILogger<ChatCommandHandler> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _ai = ai;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<ZimBotChatDto> Handle(ChatCommand request, CancellationToken ct)
    {
        ZimBotAccess.EnsureCanChat(_currentUser, request.TenantId, request.StudentId);
        await _db.SetSessionTenantIdAsync(request.TenantId, ct).ConfigureAwait(false);

        ApplicationUser student = await LoadStudentAsync(request, ct).ConfigureAwait(false);
        Module? module = await LoadModuleAsync(request, ct).ConfigureAwait(false);
        bool hintMode = request.InAssessment || AssessmentHintDetector.IsAnswerRequest(request.Message);
        string language = ZimBotLanguage.Resolve(
            string.IsNullOrWhiteSpace(request.Language) ? student.PreferredLanguage : request.Language);
        string systemPrompt = ZimBotPromptBuilder.Build(language, module, hintMode);

        string raw = await _ai.ChatAsync(systemPrompt, request.Message, ct).ConfigureAwait(false);
        (string reply, bool usedFallback, bool lowConfidence) = ZimBotReplyComposer.Compose(raw);

        ZimBotInteraction row = await PersistAsync(
                request, module, language, hintMode, usedFallback, lowConfidence, reply, ct)
            .ConfigureAwait(false);
        await PublishFallbackIfNeededAsync(request, usedFallback, ct).ConfigureAwait(false);

        _logger.LogInformation(
            "ZimBot chat stored for student {StudentId} hint={Hint} fallback={Fallback} lowConfidence={Low}.",
            request.StudentId,
            hintMode,
            usedFallback,
            lowConfidence);
        return ZimBotMapper.ToChatDto(row);
    }

    private async Task<ApplicationUser> LoadStudentAsync(ChatCommand request, CancellationToken ct)
    {
        ApplicationUser? student = await _db.Users
            .FirstOrDefaultAsync(
                u => u.Id == request.StudentId
                    && u.TenantId == request.TenantId
                    && u.Role == UserRole.Student,
                ct)
            .ConfigureAwait(false);
        if (student is null)
            throw new NotFoundException(nameof(ApplicationUser), request.StudentId);

        return student;
    }

    private async Task<Module?> LoadModuleAsync(ChatCommand request, CancellationToken ct)
    {
        if (request.ModuleId is Guid moduleId)
        {
            Module? module = await _db.Modules
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == moduleId && m.TenantId == request.TenantId, ct)
                .ConfigureAwait(false);
            if (module is null)
                throw new NotFoundException(nameof(Module), moduleId);

            return module;
        }

        return await ResolveCurrentModuleAsync(request, ct).ConfigureAwait(false);
    }

    private async Task PublishFallbackIfNeededAsync(ChatCommand request, bool usedFallback, CancellationToken ct)
    {
        if (!usedFallback)
            return;

        await _publisher
            .Publish(
                new ZimBotAiUnavailableNotification(request.TenantId, request.StudentId, request.Message),
                ct)
            .ConfigureAwait(false);
    }

    private async Task<Module?> ResolveCurrentModuleAsync(ChatCommand request, CancellationToken ct)
    {
        Guid? moduleId = await _db.StudentProgresses
            .AsNoTracking()
            .Where(p => p.TenantId == request.TenantId && p.StudentId == request.StudentId)
            .OrderBy(p => p.IsCompleted)
            .ThenByDescending(p => p.UpdatedAt)
            .Select(p => (Guid?)p.ModuleId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (moduleId is null)
            return null;

        return await _db.Modules
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.TenantId == request.TenantId, ct)
            .ConfigureAwait(false);
    }

    private async Task<ZimBotInteraction> PersistAsync(
        ChatCommand request,
        Module? module,
        string language,
        bool hintMode,
        bool usedFallback,
        bool lowConfidence,
        string reply,
        CancellationToken ct)
    {
        DateTime utcNow = DateTime.UtcNow;
        ZimBotInteraction row = new ZimBotInteraction
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            StudentId = request.StudentId,
            ModuleId = module?.Id,
            Question = request.Message,
            Response = reply,
            Language = language,
            UsedHintMode = hintMode,
            UsedFallback = usedFallback,
            IsLowConfidence = lowConfidence,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
        };
        await _db.ZimBotInteractions.AddAsync(row, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return row;
    }
}
