using EduZim.Application.Common.Interfaces;
using EduZim.Application.Common.Models;

namespace EduZim.Tests.Properties.Billing;

internal sealed class FakeInvoicePdfGenerator : IInvoicePdfGenerator
{
    public Task<byte[]> GeneratePdfAsync(InvoicePdfModel model, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new byte[] { 0x25, 0x50, 0x44, 0x46 }); // %PDF
    }
}
