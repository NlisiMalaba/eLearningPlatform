namespace EduZim.Application.Marketplace.Services;

public static class MarketplaceNotificationText
{
    public static string AccessRequested(string packTitle) =>
        $"A teacher requested access to your marketplace content pack \"{packTitle}\".";

    public static string PackRemoved(string packTitle) =>
        $"Your marketplace content pack \"{packTitle}\" was removed because it violated platform content policies.";
}
