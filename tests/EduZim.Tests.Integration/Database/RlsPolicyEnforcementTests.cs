using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Content.Queries.ListContent;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EduZim.Tests.Integration.Database;

[Collection(PostgresRlsCollection.Name)]
public sealed class RlsPolicyEnforcementTests
{
    private readonly PostgresRlsFixture _fixture;

    public RlsPolicyEnforcementTests(PostgresRlsFixture fixture)
    {
        _fixture = fixture;
    }

    [SkippableFact]
    public async Task Ef_query_with_tenant_A_session_does_not_return_tenant_B_rows()
    {
        _fixture.EnsureDockerAvailable();
        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await db.SetSessionTenantIdAsync(_fixture.TenantAId);

        List<Guid> ids = await db.ContentItems.AsNoTracking()
            .Select(c => c.Id)
            .ToListAsync();

        Assert.Contains(_fixture.ContentAId, ids);
        Assert.DoesNotContain(_fixture.ContentBId, ids);
        Assert.All(ids, id => Assert.NotEqual(_fixture.ContentBId, id));
    }

    [SkippableFact]
    public async Task Ef_query_with_tenant_B_session_does_not_return_tenant_A_rows()
    {
        _fixture.EnsureDockerAvailable();
        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await db.SetSessionTenantIdAsync(_fixture.TenantBId);

        List<Guid> ids = await db.ContentItems.AsNoTracking()
            .Select(c => c.Id)
            .ToListAsync();

        Assert.Contains(_fixture.ContentBId, ids);
        Assert.DoesNotContain(_fixture.ContentAId, ids);
    }

    [SkippableFact]
    public async Task Insert_for_other_tenant_is_rejected_when_session_tenant_is_set()
    {
        _fixture.EnsureDockerAvailable();
        await using AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope();
        EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
        await db.SetSessionTenantIdAsync(_fixture.TenantAId);

        DateTime now = DateTime.UtcNow;
        db.ContentItems.Add(new ContentItem
        {
            Id = Guid.NewGuid(),
            TenantId = _fixture.TenantBId,
            Title = "Cross-tenant insert",
            Type = ContentType.Pdf,
            StorageKey = "content/cross.pdf",
            FileSizeBytes = 1,
            Language = "en",
            Status = ContentStatus.Draft,
            UploadedByUserId = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now,
        });

        PostgresException ex = await Assert.ThrowsAsync<PostgresException>(() => db.SaveChangesAsync());
        Assert.Equal("42501", ex.SqlState);
    }

    [SkippableFact]
    public async Task List_content_http_as_tenant_A_never_includes_tenant_B_items()
    {
        _fixture.EnsureDockerAvailable();
        using HttpClient client = _fixture.Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        string token;
        await using (AsyncServiceScope scope = _fixture.Factory.Services.CreateAsyncScope())
        {
            IAccessTokenIssuer issuer = scope.ServiceProvider.GetRequiredService<IAccessTokenIssuer>();
            token = issuer.IssueForUser(_fixture.TenantAAdmin).Token;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/v1.0/content?tenantId={_fixture.TenantAId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<ContentListItemDto>? items =
            await response.Content.ReadFromJsonAsync<List<ContentListItemDto>>();
        Assert.NotNull(items);
        Assert.Contains(items, i => i.Id == _fixture.ContentAId);
        Assert.DoesNotContain(items, i => i.Id == _fixture.ContentBId);
    }
}
