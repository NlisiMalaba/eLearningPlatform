using EduZim.Domain.Enums;

namespace EduZim.Application.Tenants.Models;

public sealed record TenantDashboardDto(
    int EnrolledStudentsCount,
    int ActiveTeachersCount,
    SubscriptionStatus? SubscriptionStatus,
    long StorageUsageBytes);
