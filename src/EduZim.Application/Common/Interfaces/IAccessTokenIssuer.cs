using EduZim.Domain.Entities;

namespace EduZim.Application.Common.Interfaces;

public interface IAccessTokenIssuer
{
    (string Token, DateTimeOffset ExpiresAt) IssueForUser(ApplicationUser user);
}
