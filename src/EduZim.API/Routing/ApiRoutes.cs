namespace EduZim.API.Routing;

/// <summary>Resolved v1 paths for Location headers and configuration defaults (match <c>api/v{{version:apiVersion}}</c> with version 1.0).</summary>
internal static class ApiRoutes
{
    public const string V1Auth = "api/v1/auth";
    public const string V1Tenants = "api/v1/tenants";
    public const string V1Billing = "api/v1/billing";
    public const string V1Content = "api/v1/content";
    public const string V1Modules = "api/v1/modules";
    public const string V1Assessments = "api/v1/assessments";
    public const string V1Adaptive = "api/v1/adaptive";
    public const string V1Gamification = "api/v1/gamification";
    public const string V1Notifications = "api/v1/notifications";
    public const string V1Users = "api/v1/users";
    public const string V1ZimBot = "api/v1/zimbot";
    public const string V1Classrooms = "api/v1/classrooms";
    public const string V1Sync = "api/v1/sync";
    public const string V1Marketplace = "api/v1/marketplace";
}
