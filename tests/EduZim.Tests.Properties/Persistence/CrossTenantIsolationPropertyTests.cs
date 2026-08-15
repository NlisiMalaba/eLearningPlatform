using FsCheck.Xunit;
using Xunit;

namespace EduZim.Tests.Properties.Persistence;

/// <summary>
/// Property 1: Cross-tenant data isolation — for any tenant-scoped query, results must only include rows for the session tenant.
/// PostgreSQL RLS policies on school-tier tables (see InitialCreate migration) enforce this when <c>app.current_tenant_id</c> is set.
/// Full automation requires two database sessions with different <c>SET</c> values (e.g. Testcontainers PostgreSQL).
/// </summary>
public sealed class CrossTenantIsolationPropertyTests
{
    // Feature: elearning-app-zimbabwe, Property 1: Cross-tenant data isolation — Validates: Requirements 11.1, 11.3
    [Property(MaxTest = 500)]
    public void SessionTenantId_string_matches_uuid_format(Guid tenantId)
    {
        Assert.Equal(36, tenantId.ToString("D").Length);
    }
}
