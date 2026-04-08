namespace EduZim.Application.Common.Interfaces;

public interface IAiService
{
    Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}
