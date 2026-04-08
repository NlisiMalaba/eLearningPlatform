namespace EduZim.Application.Common.Interfaces;

public interface IAuditLogWriter
{
    Task WriteAsync(AuditLogWrite entry, CancellationToken cancellationToken = default);
}
