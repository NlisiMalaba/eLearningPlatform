using EduZim.Application.Common.Configuration;
using EduZim.Application.Common.Interfaces;
using EduZim.Application.Tenants.Commands.UploadBrandingLogo;
using EduZim.Domain.Entities;
using EduZim.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MockQueryable.Moq;
using Moq;

namespace EduZim.Tests.Unit.Handlers;

public sealed class UploadBrandingLogoCommandHandlerTests
{
    [Fact]
    public async Task Stores_logo_key_and_returns_signed_url()
    {
        Guid tenantId = Guid.NewGuid();
        Tenant tenant = new()
        {
            Id = tenantId,
            Name = "School",
            Branding = new BrandingSettings { SchoolName = "School", PrimaryColour = "#1976D2" },
        };
        Mock<IEduZimDbContext> db = new();
        db.Setup(x => x.SetSessionTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        db.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        db.Setup(x => x.Tenants).Returns(new List<Tenant> { tenant }.AsQueryable().BuildMockDbSet().Object);

        Mock<ICurrentUser> user = new();
        user.Setup(u => u.Role).Returns(UserRole.SchoolAdmin);
        user.Setup(u => u.TenantId).Returns(tenantId);

        Mock<IStorageService> storage = new();
        storage.Setup(s => s.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync("key");
        storage.Setup(s => s.GetSignedUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("https://cdn.example/logo");

        UploadBrandingLogoCommandHandler handler = new(
            db.Object,
            user.Object,
            storage.Object,
            Options.Create(new ContentStorageOptions { SignedUrlExpiryMinutes = 60 }),
            NullLogger<UploadBrandingLogoCommandHandler>.Instance);

        await using MemoryStream content = new([1, 2, 3]);
        string url = await handler.Handle(
            new UploadBrandingLogoCommand(tenantId, 3, "image/png", content),
            CancellationToken.None);

        Assert.Equal("https://cdn.example/logo", url);
        Assert.EndsWith("/branding/logo", tenant.Branding.LogoUrl);
    }
}
