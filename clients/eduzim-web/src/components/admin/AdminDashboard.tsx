"use client";

import { useQuery } from "@tanstack/react-query";
import { formatStorageBytes } from "@/lib/admin/branding";
import {
  getTenantDashboard,
  getTenantDetails,
  type SubscriptionStatus,
} from "@/lib/admin/tenantService";
import type { MessageKey } from "@/lib/i18n/messages";
import { t } from "@/lib/i18n/t";

type AdminDashboardProps = {
  tenantId: string;
};

const STATUS_KEYS: Record<SubscriptionStatus, MessageKey> = {
  Active: "admin.subscription.Active",
  GracePeriod: "admin.subscription.GracePeriod",
  Suspended: "admin.subscription.Suspended",
  Cancelled: "admin.subscription.Cancelled",
};

export function AdminDashboard({ tenantId }: AdminDashboardProps) {
  const dashboardQuery = useQuery({
    queryKey: ["tenant-dashboard", tenantId],
    queryFn: () => getTenantDashboard(tenantId),
  });
  const tenantQuery = useQuery({
    queryKey: ["tenant-details", tenantId],
    queryFn: () => getTenantDetails(tenantId),
  });

  if (dashboardQuery.isLoading || tenantQuery.isLoading) {
    return <p>{t("admin.dashboard.loading")}</p>;
  }

  if (dashboardQuery.isError || !dashboardQuery.data) {
    return <p role="alert">{t("admin.dashboard.error")}</p>;
  }

  const data = dashboardQuery.data;
  const schoolName = tenantQuery.data?.schoolName ?? t("admin.dashboard.title");
  const colour = tenantQuery.data?.primaryColour ?? "#0B6E4F";
  const statusKey: MessageKey = data.subscriptionStatus
    ? STATUS_KEYS[data.subscriptionStatus]
    : "admin.subscription.none";

  return (
    <div className="flex flex-col gap-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{schoolName}</h1>
        <p className="mt-2 max-w-xl text-zinc-600">{t("admin.dashboard.subtitle")}</p>
      </header>
      <div className="grid gap-4 sm:grid-cols-2">
        <StatCard label={t("admin.dashboard.students")} value={String(data.enrolledStudentsCount)} colour={colour} />
        <StatCard label={t("admin.dashboard.teachers")} value={String(data.activeTeachersCount)} colour={colour} />
        <StatCard label={t("admin.dashboard.subscription")} value={t(statusKey)} colour={colour} />
        <StatCard
          label={t("admin.dashboard.storage")}
          value={formatStorageBytes(data.storageUsageBytes)}
          colour={colour}
        />
      </div>
    </div>
  );
}

function StatCard({ label, value, colour }: { label: string; value: string; colour: string }) {
  return (
    <article className="rounded-xl border border-zinc-200 bg-white p-4" style={{ borderTopColor: colour, borderTopWidth: 4 }}>
      <p className="text-sm text-zinc-600">{label}</p>
      <p className="mt-2 text-2xl font-semibold">{value}</p>
    </article>
  );
}
