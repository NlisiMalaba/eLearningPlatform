using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;

namespace EduZim.Tests.Properties.Notifications;

internal sealed class RecordingSmsService : ISmsService
{
    public bool Succeed { get; set; } = true;

    public List<(string Phone, string Message)> Sent { get; } = [];

    public Task<SmsResult> SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        Sent.Add((phoneNumber, message));
        return Task.FromResult(new SmsResult { Success = Succeed });
    }
}
