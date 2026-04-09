using MediatR;

namespace EduZim.Application.Identity.Queries.SsoRedirect;

public sealed record SsoRedirectQuery(Guid TenantId) : IRequest<Uri>;
