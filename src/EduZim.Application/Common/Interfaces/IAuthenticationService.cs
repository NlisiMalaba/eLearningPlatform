using EduZim.Application.Common.Auth;

namespace EduZim.Application.Common.Interfaces;

public interface IAuthenticationService
{
    Task<SignInWithPasswordResult> SignInWithPasswordAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}
