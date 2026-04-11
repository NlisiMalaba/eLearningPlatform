using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EduZim.Application.Content.Commands.CreateModule;

public sealed class CreateModuleCommandHandler : IRequestHandler<CreateModuleCommand, Guid>
{
    private readonly IEduZimDbContext _db;
    private readonly ILogger<CreateModuleCommandHandler> _logger;

    public CreateModuleCommandHandler(IEduZimDbContext db, ILogger<CreateModuleCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateModuleCommand request, CancellationToken cancellationToken)
    {
        await _db.SetSessionTenantIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();
        var module = new Module
        {
            Id = id,
            TenantId = request.TenantId,
            Title = request.Title,
            Grade = request.Grade,
            Subject = request.Subject,
            SequenceOrder = request.SequenceOrder,
            IsRequired = request.IsRequired,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _db.Modules.AddAsync(module, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Created module {ModuleId} for tenant {TenantId}, grade {Grade}, subject {Subject}, sequence {Sequence}.",
            id,
            request.TenantId,
            request.Grade,
            request.Subject,
            request.SequenceOrder);

        return id;
    }
}
