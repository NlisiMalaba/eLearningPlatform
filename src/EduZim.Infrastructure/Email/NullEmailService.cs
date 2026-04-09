using EduZim.Application.Common.Interfaces;

namespace EduZim.Infrastructure.Email;

public sealed class NullEmailService : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
