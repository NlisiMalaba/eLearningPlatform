using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;

namespace EduZim.Infrastructure.Sms;

public sealed class NullSmsService : ISmsService
{
    public Task<SmsResult> SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        return Task.FromResult(new SmsResult { Success = true });
    }
}
