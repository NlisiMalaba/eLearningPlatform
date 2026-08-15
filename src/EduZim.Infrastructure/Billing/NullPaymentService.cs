using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;
using EduZim.Infrastructure.Http;
using System.Net.Http.Json;

namespace EduZim.Infrastructure.Billing;

public sealed class NullPaymentService : IPaymentService
{
    private readonly IHttpClientFactory _httpClients;

    public NullPaymentService(IHttpClientFactory httpClients)
    {
        _httpClients = httpClients;
    }

    public async Task<PaymentResult> CreateSubscriptionAsync(
        CreatePaymentRequest request,
        CancellationToken ct = default)
    {
        HttpClient client = _httpClients.CreateClient(ExternalHttpClientNames.Payment);
        if (!ExternalHttpCall.HasBaseAddress(client))
        {
            return new PaymentResult
            {
                Success = true,
                PaymentProviderReference = "local-" + (request.IdempotencyKey ?? Guid.NewGuid().ToString("N")),
            };
        }

        try
        {
            using HttpResponseMessage response = await client
                .PostAsJsonAsync("subscriptions", request, ct)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Payment provider returned {(int)response.StatusCode}.",
                };
            }

            PaymentResult? result = await response.Content
                .ReadFromJsonAsync<PaymentResult>(ct)
                .ConfigureAwait(false);
            return result ?? new PaymentResult { Success = true };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ExternalHttpCall.IsTransient(ex))
        {
            return new PaymentResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<Invoice> GenerateInvoiceAsync(
        Guid subscriptionId,
        Guid paymentId,
        CancellationToken ct = default)
    {
        HttpClient client = _httpClients.CreateClient(ExternalHttpClientNames.Payment);
        Invoice local = new()
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            PaymentId = paymentId,
            IssuedAt = DateTime.UtcNow,
        };
        if (!ExternalHttpCall.HasBaseAddress(client))
            return local;

        try
        {
            using HttpResponseMessage response = await client
                .PostAsJsonAsync("invoices", new { subscriptionId, paymentId }, ct)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return local;

            return await response.Content.ReadFromJsonAsync<Invoice>(ct).ConfigureAwait(false) ?? local;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ExternalHttpCall.IsTransient(ex))
        {
            return local;
        }
    }
}
