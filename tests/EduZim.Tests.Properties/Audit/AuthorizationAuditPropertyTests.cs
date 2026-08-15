using EduZim.API.ExceptionHandling;
using EduZim.Application.Common.Auth;
using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Exceptions;
using EduZim.Infrastructure.Audit;
using EduZim.Infrastructure.Persistence;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EduZim.Tests.Properties.Audit;

// Feature: elearning-app-zimbabwe, Property 44: Unauthorized Requests Return 403 with Audit Log
/// <summary>
/// Feature: elearning-app-zimbabwe, Property 44: Unauthorized Requests Return 403 with Audit Log — for any
/// authorisation failure that maps to tenant access denial, HTTP 403 applies and an audit record captures it (Requirement 16.5).
/// </summary>
public sealed class AuthorizationAuditPropertyTests
{
    [Property(MaxTest = 500)]
    public void Property44_TenantAccessViolation_maps_to_403_problem_details(NonEmptyString message)
    {
        var ex = new TenantAccessViolationException(message.Get);
        var (statusCode, details) = ProblemDetailsMapper.Map(ex);

        Assert.Equal(403, statusCode);
        Assert.Equal(403, details.Status);
        Assert.NotNull(details.Type);
        Assert.Contains("tenant-access", details.Type, StringComparison.Ordinal);
    }

    [Property(MaxTest = 500)]
    public async Task Property44_forbidden_response_audit_write_persists_with_forbidden_action(
        NonEmptyString path,
        Guid userId,
        Guid tenantId)
    {
        await using var context = CreateContext();
        var writer = new AuditLogWriter(context);
        var resourcePath = "/" + path.Get.TrimStart('/');

        await writer.WriteAsync(
            new AuditLogWrite(
                tenantId,
                userId,
                AuthorizationFailureAuditActions.Forbidden,
                resourcePath,
                null,
                "127.0.0.1"));

        var stored = context.AuditLogs.Single();
        Assert.Equal(AuthorizationFailureAuditActions.Forbidden, stored.Action);
        Assert.Equal(resourcePath, stored.ResourceType);
        Assert.Equal(userId, stored.UserId);
        Assert.Equal(tenantId, stored.TenantId);
    }

    private static EduZimDbContext CreateContext()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var provider = services.BuildServiceProvider();
        var dataProtection = provider.GetRequiredService<IDataProtectionProvider>();

        var options = new DbContextOptionsBuilder<EduZimDbContext>()
            .UseInMemoryDatabase("AuditProp-" + Guid.NewGuid())
            .Options;

        return new EduZimDbContext(options, dataProtection);
    }
}
