using EduZim.Application.Common.Models;

namespace EduZim.Application.Common.Interfaces;

public interface IInvoicePdfGenerator
{
    Task<byte[]> GeneratePdfAsync(InvoicePdfModel model, CancellationToken cancellationToken = default);
}
