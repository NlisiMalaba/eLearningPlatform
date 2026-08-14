using EduZim.Application.Common.Interfaces;

namespace EduZim.Tests.Properties.Notifications;

internal sealed class RecordingEmailService : IEmailService
{
    public List<(string To, string Subject, string Body)> Sent { get; } = [];

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        Sent.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}
