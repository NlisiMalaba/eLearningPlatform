using EduZim.Domain.Entities;
using EduZim.Domain.Enums;

namespace EduZim.Application.Progress.Services;

internal static class ScreenTimeSessionMaintenance
{
    public static void CloseStaleSessions(IEnumerable<StudentSession> sessions, DateOnly today, DateTime utcNow)
    {
        foreach (StudentSession session in sessions)
        {
            if (session.Status == SessionStatus.Active && session.SessionDate != today)
            {
                session.Status = SessionStatus.Paused;
                session.UpdatedAt = utcNow;
            }
        }
    }
}
