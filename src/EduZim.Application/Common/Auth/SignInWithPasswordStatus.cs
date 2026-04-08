namespace EduZim.Application.Common.Auth;

public enum SignInWithPasswordStatus
{
    Success,
    InvalidCredentials,
    LockedOut,
    EmailNotConfirmed,
}
