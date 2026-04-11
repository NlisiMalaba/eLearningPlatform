using Asp.Versioning;
using EduZim.API.Billing;
using EduZim.API.Contracts;
using EduZim.API.Routing;
using EduZim.Application.Billing.Commands.CreateSubscription;
using EduZim.Application.Billing.Queries.GetSubscriptionInvoiceDownload;
using EduZim.Application.Billing.Queries.ListSubscriptionInvoices;
using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants;
using EduZim.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace EduZim.API.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/billing")]
[Tags("Billing")]
[Authorize]
public sealed class BillingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;
    private readonly IBillingStripeWebhookProcessor _stripeWebhook;
    private readonly IOptions<StripeOptions> _stripeOptions;
    private readonly ILogger<BillingController> _logger;

    public BillingController(
        IMediator mediator,
        ICurrentUser currentUser,
        IBillingStripeWebhookProcessor stripeWebhook,
        IOptions<StripeOptions> stripeOptions,
        ILogger<BillingController> logger)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _stripeWebhook = stripeWebhook;
        _stripeOptions = stripeOptions;
        _logger = logger;
    }

    [HttpPost("subscriptions")]
    [ProducesResponseType(typeof(CreateSubscriptionResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (ValidateBillingTenantContext(request.TenantId) is { } err)
            return err;

        var tenantId = ResolveBillingTenantId(request.TenantId);

        TenantAccessHelper.EnsureCanManageBilling(_currentUser, tenantId);

        var id = await _mediator.Send(
            new CreateSubscriptionCommand(tenantId, request.Cycle, request.StudentCount),
            cancellationToken);

        return Created(
            $"/{ApiRoutes.V1Billing}/subscriptions/{id}",
            new CreateSubscriptionResponse { SubscriptionId = id });
    }

    [HttpPost("webhooks/stripe")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        var secret = _stripeOptions.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogError("Stripe webhook received but Stripe:WebhookSecret is not configured.");
            return Problem(
                title: "Stripe webhook not configured",
                detail: "Set Stripe:WebhookSecret to the signing secret from the Stripe dashboard.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        string json;
        using (var reader = new StreamReader(Request.Body))
        {
            json = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(json))
            return BadRequest("Empty body.");

        Event stripeEvent;
        try
        {
            var signature = Request.Headers["Stripe-Signature"].ToString();
            stripeEvent = EventUtility.ConstructEvent(json, signature, secret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature or payload.");
            return BadRequest("Invalid Stripe signature or payload.");
        }

        var notification = StripeBillingEventMapper.Map(stripeEvent);
        await _stripeWebhook.ProcessAsync(notification, cancellationToken).ConfigureAwait(false);
        return Ok();
    }

    [HttpGet("invoices/{subscriptionId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionInvoiceItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInvoicesForSubscription(
        Guid subscriptionId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (ValidateBillingTenantContext(tenantId) is { } err)
            return err;

        var resolvedTenantId = ResolveBillingTenantId(tenantId);

        TenantAccessHelper.EnsureCanManageBilling(_currentUser, resolvedTenantId);

        var items = await _mediator.Send(
            new ListSubscriptionInvoicesQuery(resolvedTenantId, subscriptionId),
            cancellationToken);

        var response = items.Select(i => new SubscriptionInvoiceItemResponse
        {
            Id = i.Id,
            SubscriptionId = i.SubscriptionId,
            PaymentId = i.PaymentId,
            IssuedAtUtc = i.IssuedAtUtc,
            FileName = i.FileName,
        }).ToList();

        return Ok(response);
    }

    [HttpGet("invoices/{invoiceId:guid}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadInvoice(
        Guid invoiceId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        if (ValidateBillingTenantContext(tenantId) is { } err)
            return err;

        var resolvedTenantId = ResolveBillingTenantId(tenantId);

        TenantAccessHelper.EnsureCanManageBilling(_currentUser, resolvedTenantId);

        var file = await _mediator.Send(
            new GetSubscriptionInvoiceDownloadQuery(resolvedTenantId, invoiceId),
            cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Platform admins must supply <paramref name="optionalTenantId"/>; school users may omit it.</summary>
    private IActionResult? ValidateBillingTenantContext(Guid? optionalTenantId)
    {
        if (_currentUser.Role == UserRole.PlatformAdmin)
        {
            if (optionalTenantId is null)
            {
                return Problem(
                    title: "Tenant required",
                    detail: "Provide tenantId (query or request body) when using platform administrator credentials.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return null;
        }

        if (_currentUser.TenantId is null)
            return Forbid();

        if (optionalTenantId.HasValue && optionalTenantId.Value != _currentUser.TenantId.Value)
        {
            return Problem(
                title: "Tenant mismatch",
                detail: "tenantId does not match the authenticated user's tenant.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        return null;
    }

    private Guid ResolveBillingTenantId(Guid? optionalTenantId)
    {
        return _currentUser.Role == UserRole.PlatformAdmin
            ? optionalTenantId!.Value
            : _currentUser.TenantId!.Value;
    }
}
