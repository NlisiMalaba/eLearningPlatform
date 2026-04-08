using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Infrastructure.Persistence;

namespace EduZim.Infrastructure.Audit;

public sealed class AuditLogWriter : IAuditLogWriter
{
    private readonly EduZimDbContext _db;

    public AuditLogWriter(EduZimDbContext db)
    {
        _db = db;
    }

    public async Task WriteAsync(AuditLogWrite entry, CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = entry.TenantId,
            UserId = entry.UserId,
            Action = entry.Action,
            ResourceType = entry.ResourceType,
            ResourceId = entry.ResourceId,
            Timestamp = DateTime.UtcNow,
            IpAddress = entry.IpAddress,
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
