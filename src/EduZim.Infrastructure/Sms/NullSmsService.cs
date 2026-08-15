using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;
using EduZim.Infrastructure.Http;
using System.Net.Http.Json;

namespace EduZim.Infrastructure.Sms;

public sealed class NullSmsService : ISmsService
{
    private readonly IHttpClientFactory _httpClients;

    public NullSmsService(IHttpClientFactory httpClients)
    {
        _httpClients = httpClients;
    }

    public async Task<SmsResult> SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        HttpClient client = _httpClients.CreateClient(ExternalHttpClientNames.Sms);
        if (!ExternalHttpCall.HasBaseAddress(client))
            return new SmsResult { Success = true };

        try
        {
            using HttpResponseMessage response = await client
                .PostAsJsonAsync("messages", new { phoneNumber, message }, ct)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return new SmsResult { Success = true };

            return new SmsResult
            {
                Success = false,
                ErrorMessage = $"SMS gateway returned {(int)response.StatusCode}.",
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ExternalHttpCall.IsTransient(ex))
        {
            return new SmsResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}
