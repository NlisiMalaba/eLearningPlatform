using EduZim.Application.Common.Interfaces;
using EduZim.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduZim.Infrastructure.Jobs;

/// <summary>Builds a printable PDF certificate and stores it via <see cref="IStorageService"/>.</summary>
public sealed class BadgeCertificateGenerationService
{
    private readonly IEduZimDbContext _db;
    private readonly IStorageService _storage;
    private readonly ILogger<BadgeCertificateGenerationService> _logger;

    static BadgeCertificateGenerationService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public BadgeCertificateGenerationService(
        IEduZimDbContext db,
        IStorageService storage,
        ILogger<BadgeCertificateGenerationService> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task GenerateAndStoreAsync(Guid tenantId, Guid studentId, Guid badgeId, CancellationToken ct)
    {
        await _db.SetSessionTenantIdAsync(tenantId, ct).ConfigureAwait(false);

        Badge? badge = await _db.Badges
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == badgeId && b.TenantId == tenantId && b.StudentId == studentId, ct)
            .ConfigureAwait(false);
        if (badge is null)
        {
            _logger.LogWarning(
                "Badge {BadgeId} not found for student {StudentId}; skipping certificate.",
                badgeId,
                studentId);
            return;
        }

        byte[] pdf = RenderPdf(badge);
        string key = $"{tenantId}/certificates/{studentId}/{badgeId}.pdf";
        await using MemoryStream stream = new(pdf);
        await _storage.UploadAsync(key, stream, "application/pdf", ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Stored progress certificate for badge {BadgeId} at {StorageKey}.",
            badgeId,
            key);
    }

    private static byte[] RenderPdf(Badge badge)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Column(header =>
                {
                    header.Item().Text("EduZim").SemiBold().FontSize(22);
                    header.Item().Text("Progress Certificate").FontSize(16);
                });

                page.Content().PaddingTop(2, Unit.Centimetre).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text("This certificate recognises a learning milestone.");
                    column.Item().Text($"Badge: {badge.Type}");
                    column.Item().Text($"Earned (UTC): {badge.EarnedAt:yyyy-MM-dd}");
                    column.Item().Text($"Certificate ID: {badge.Id}");
                });

                page.Footer().AlignCenter().Text($"Tenant {badge.TenantId}");
            });
        }).GeneratePdf();
    }
}
