namespace EduZim.Application.Sync.Services;

/// <summary>Last-write-wins by timestamp (Property 33 / requirement 12.5).</summary>
public static class SyncConflictRules
{
    public static bool LocalWins(DateTime localTimestamp, DateTime serverTimestamp) =>
        localTimestamp > serverTimestamp;

    public static DateTime RetainedTimestamp(DateTime localTimestamp, DateTime serverTimestamp) =>
        LocalWins(localTimestamp, serverTimestamp) ? localTimestamp : serverTimestamp;
}
