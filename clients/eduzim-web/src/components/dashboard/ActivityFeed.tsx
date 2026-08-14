import { t } from "@/lib/i18n/t";
import type { RecentActivity } from "@/lib/dashboard/types";

type ActivityFeedProps = {
  items: readonly RecentActivity[];
};

export function ActivityFeed({ items }: ActivityFeedProps) {
  if (items.length === 0) {
    return <p className="text-sm text-zinc-600">{t("dashboard.activity.empty")}</p>;
  }

  return (
    <ol className="flex flex-col gap-3">
      {items.map((item) => (
        <li key={item.id} className="rounded-lg border border-zinc-200 bg-white px-3 py-2">
          <p className="text-sm font-medium">
            {item.kind === "ModuleCompleted"
              ? t("dashboard.activity.module")
              : t("dashboard.activity.badge")}
          </p>
          <p className="text-sm text-zinc-700">{activityTitle(item)}</p>
          <time className="text-xs text-zinc-500" dateTime={item.occurredAt}>
            {formatWhen(item.occurredAt)}
          </time>
        </li>
      ))}
    </ol>
  );
}

function activityTitle(item: RecentActivity): string {
  if (item.kind === "BadgeEarned") {
    return badgeLabel(item.title);
  }

  return item.title;
}

function badgeLabel(type: string): string {
  switch (type) {
    case "FirstModule":
      return t("dashboard.badge.FirstModule");
    case "FiveConsecutiveDays":
      return t("dashboard.badge.FiveConsecutiveDays");
    case "SubjectMastery":
      return t("dashboard.badge.SubjectMastery");
    case "GradeCompletion":
      return t("dashboard.badge.GradeCompletion");
    default:
      return type;
  }
}

function formatWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }

  return date.toLocaleString();
}
