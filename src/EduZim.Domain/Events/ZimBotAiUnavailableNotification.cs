using MediatR;

namespace EduZim.Domain.Events;

/// <summary>Raised when the ZimBot AI circuit is open or the model cannot be reached (requirement 5.8).</summary>
public sealed record ZimBotAiUnavailableNotification(
    Guid TenantId,
    Guid StudentId,
    string Question) : INotification;
