using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EduZim.Application.Billing.Webhooks;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Integration.Support;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Integration.Database;

[Collection(PostgresRlsCollection.Name)]
public sealed class BillingWebhookIdempotencyTests
{
    private readonly PostgresRlsFixture _fixture;

    public BillingWebhookIdempotencyTests(PostgresRlsFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Replaying_the_same_stripe_invoice_paid_event_creates_one_invoice_and_one_activation()
    {
        _fixture.EnsureDockerAvailable();

        Guid subscriptionId = Guid.NewGuid();
        DateTime expiredPeriod = DateTime.UtcNow.AddDays(-2);
        await SeedSuspendedSubscriptionAsync(subscriptionId, expiredPeriod);

        string eventId = "evt_idem_" + Guid.NewGuid().ToString("N");
        string json = StripeInvoicePaidTestPayload.Create(
            eventId,
            _fixture.TenantAId,
            subscriptionId,
            amountCents: 2200,
            invoiceId: "in_" + Guid.NewGuid().ToString("N")[..24]);
        string signature = StripeWebhookTestSignature.CreateHeader(
            json,
            PostgresRlsFixture.StripeWebhookSecret);

        using HttpClient client = _fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage first = await PostStripeWebhookAsync(client, json, signature);
        HttpResponseMessage second = await PostStripeWebhookAsync(client, json, signature);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await db.SetSessionTenantIdAsync(_fixture.TenantAId);

        Subscription subscription = await db.Subscriptions.AsNoTracking()
            .SingleAsync(s => s.Id == subscriptionId);
        List<Payment> payments = await db.Payments.AsNoTracking()
            .Where(p => p.SubscriptionId == subscriptionId)
            .ToListAsync();
        int invoiceCount = await db.SubscriptionInvoices.CountAsync(i => i.SubscriptionId == subscriptionId);

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Null(subscription.GracePeriodEnd);
        Assert.True(subscription.CurrentPeriodEnd > DateTime.UtcNow.AddMinutes(-1));
        Assert.Single(payments);
        Assert.Equal(eventId, payments[0].IdempotencyKey);
        Assert.Equal(1, invoiceCount);
    }

    [SkippableFact]
    public async Task Replaying_the_same_processor_notification_does_not_extend_period_twice()
    {
        _fixture.EnsureDockerAvailable();

        Guid subscriptionId = Guid.NewGuid();
        DateTime expiredPeriod = DateTime.UtcNow.AddDays(-3);
        await SeedSuspendedSubscriptionAsync(subscriptionId, expiredPeriod);

        var notification = new BillingStripeNotification(
            BillingStripeNotificationKind.PaymentSucceeded,
            _fixture.TenantAId,
            subscriptionId,
            22m,
            "USD",
            "pi_replay_" + Guid.NewGuid().ToString("N")[..12],
            "idem-replay-" + Guid.NewGuid().ToString("N"),
            null);

        await using (AsyncServiceScope processScope = _fixture.Factory.Services.CreateAsyncScope())
        {
            IBillingStripeWebhookProcessor processor =
                processScope.ServiceProvider.GetRequiredService<IBillingStripeWebhookProcessor>();
            await processor.ProcessAsync(notification, CancellationToken.None);
            await processor.ProcessAsync(notification, CancellationToken.None);
        }

        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await db.SetSessionTenantIdAsync(_fixture.TenantAId);

        Subscription subscription = await db.Subscriptions.AsNoTracking()
            .SingleAsync(s => s.Id == subscriptionId);
        DateTime periodEndAfterReplay = subscription.CurrentPeriodEnd;

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(1, await db.Payments.CountAsync(p => p.SubscriptionId == subscriptionId));
        Assert.Equal(1, await db.SubscriptionInvoices.CountAsync(i => i.SubscriptionId == subscriptionId));

        // A second non-idempotent success would extend CurrentPeriodEnd by another month.
        Assert.True(periodEndAfterReplay < DateTime.UtcNow.AddMonths(1).AddDays(2));
        Assert.True(periodEndAfterReplay > DateTime.UtcNow.AddDays(20));
    }

    private async Task SeedSuspendedSubscriptionAsync(Guid subscriptionId, DateTime expiredPeriodEnd)
    {
        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await db.SetSessionTenantIdAsync(_fixture.TenantAId);

        db.Subscriptions.Add(new Subscription
        {
            Id = subscriptionId,
            TenantId = _fixture.TenantAId,
            Cycle = BillingCycle.Monthly,
            Status = SubscriptionStatus.Suspended,
            CurrentPeriodStart = expiredPeriodEnd.AddMonths(-1),
            CurrentPeriodEnd = expiredPeriodEnd,
            GracePeriodEnd = expiredPeriodEnd.AddDays(7),
            StudentCount = 2,
        });
        await db.SaveChangesAsync();
    }

    private static async Task<HttpResponseMessage> PostStripeWebhookAsync(
        HttpClient client,
        string json,
        string stripeSignature)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/billing/webhooks/stripe")
        {
            Content = content,
        };
        request.Headers.TryAddWithoutValidation("Stripe-Signature", stripeSignature);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await client.SendAsync(request);
    }
}
