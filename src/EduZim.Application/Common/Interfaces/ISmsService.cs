using EduZim.Application.Common.Models;

namespace EduZim.Application.Common.Interfaces;

public interface ISmsService
{
    Task<SmsResult> SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
