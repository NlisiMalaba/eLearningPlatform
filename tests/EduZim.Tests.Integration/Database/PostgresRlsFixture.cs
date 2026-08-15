using DotNet.Testcontainers.Builders;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using EduZim.Infrastructure.Persistence;
using EduZim.Tests.Integration.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EduZim.Tests.Integration.Database;

public sealed class PostgresRlsFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;

    public EduZimWebApplicationFactory Factory { get; private set; } = null!;

    public bool DockerAvailable { get; private set; }

    public string? DockerSkipReason { get; private set; }

    public Guid TenantAId { get; } = Guid.NewGuid();

    public Guid TenantBId { get; } = Guid.NewGuid();

    public Guid ContentAId { get; } = Guid.NewGuid();

    public Guid ContentBId { get; } = Guid.NewGuid();

    public ApplicationUser TenantAAdmin { get; private set; } = null!;

    /// <summary>Signing secret injected into the test host for Stripe webhook HMAC verification.</summary>
    public const string StripeWebhookSecret = "whsec_eduzim_integration_test_secret";

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("eduzim_rls")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();
        }
        catch (Exception ex) when (ex is DockerUnavailableException
                                   || ex.GetBaseException() is DockerUnavailableException)
        {
            DockerSkipReason = $"Docker is required for Testcontainers integration tests: {ex.Message}";
            return;
        }

        Factory = new EduZimWebApplicationFactory(
            _container.GetConnectionString(),
            new Dictionary<string, string?>
            {
                ["Stripe:WebhookSecret"] = StripeWebhookSecret,
            });

        await using (AsyncServiceScope scope = Factory.Services.CreateAsyncScope())
        {
            EduZimDbContext db = scope.ServiceProvider.GetRequiredService<EduZimDbContext>();
            await db.Database.MigrateAsync();
            await SeedAsync(db);
        }

        await ForceRowLevelSecurityAsync();
        DockerAvailable = true;
    }

    public void EnsureDockerAvailable()
    {
        Skip.If(!DockerAvailable, DockerSkipReason ?? "Docker is required for Testcontainers integration tests.");
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        if (_container is not null)
            await _container.DisposeAsync();
    }

    private async Task SeedAsync(EduZimDbContext db)
    {
        DateTime now = DateTime.UtcNow;
        db.Tenants.AddRange(
            CreateTenant(TenantAId, "School A", now),
            CreateTenant(TenantBId, "School B", now));

        TenantAAdmin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            TenantId = TenantAId,
            Role = UserRole.SchoolAdmin,
            UserName = "admin-a@eduzim.test",
            Email = "admin-a@eduzim.test",
            EmailConfirmed = true,
            FullName = "Admin A",
            NormalizedUserName = "ADMIN-A@EDUZIM.TEST",
            NormalizedEmail = "ADMIN-A@EDUZIM.TEST",
            SecurityStamp = Guid.NewGuid().ToString(),
        };
        db.Users.Add(TenantAAdmin);

        db.ContentItems.AddRange(
            CreateContent(ContentAId, TenantAId, "Tenant A module", now),
            CreateContent(ContentBId, TenantBId, "Tenant B module", now));

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Table owners bypass ENABLE ROW LEVEL SECURITY unless FORCE is applied; the API connects as owner.
    /// </summary>
    private async Task ForceRowLevelSecurityAsync()
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DO $$
            DECLARE r record;
            BEGIN
              FOR r IN
                SELECT c.relname AS table_name
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'public'
                  AND c.relkind = 'r'
                  AND c.relrowsecurity
              LOOP
                EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', r.table_name);
              END LOOP;
            END $$;
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static Tenant CreateTenant(Guid id, string name, DateTime createdAt) =>
        new()
        {
            Id = id,
            Name = name,
            Tier = TenantTier.School,
            Status = TenantStatus.Active,
            Branding = new BrandingSettings { SchoolName = name, PrimaryColour = "#1976D2" },
            CreatedAt = createdAt,
        };

    private static ContentItem CreateContent(Guid id, Guid tenantId, string title, DateTime createdAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            Title = title,
            Type = ContentType.Pdf,
            StorageKey = $"content/{id:N}.pdf",
            FileSizeBytes = 1024,
            Language = "en",
            Status = ContentStatus.Published,
            UploadedByUserId = Guid.NewGuid(),
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
}
