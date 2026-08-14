import { t } from "@/lib/i18n/t";
import type { StudentBadge } from "@/lib/dashboard/types";

type GamificationSummaryProps = {
  totalPoints: number;
  badges: readonly StudentBadge[];
  leaderboardRank: number | null;
};

export function GamificationSummary({
  totalPoints,
  badges,
  leaderboardRank,
}: GamificationSummaryProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-3">
      <Stat label={t("dashboard.points")} value={String(totalPoints)} />
      <Stat
        label={t("dashboard.leaderboard")}
        value={
          leaderboardRank
            ? t("dashboard.rank").replace("{value}", String(leaderboardRank))
            : t("dashboard.unranked")
        }
      />
      <section className="rounded-xl border border-zinc-200 bg-white p-4 sm:col-span-3">
        <h2 className="text-sm font-semibold">{t("dashboard.badges")}</h2>
        {badges.length === 0 ? (
          <p className="mt-2 text-sm text-zinc-600">{t("dashboard.badges.empty")}</p>
        ) : (
          <ul className="mt-3 flex flex-wrap gap-2">
            {badges.map((badge) => (
              <li
                key={badge.badgeId}
                className="rounded-full border border-[#0B6E4F] px-3 py-1 text-sm text-[#0B6E4F]"
              >
                {badgeLabel(badge.type)}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

function Stat({ label, value }: { label: string; value: string }) {
  return (
    <section className="rounded-xl border border-zinc-200 bg-white p-4">
      <h2 className="text-sm font-semibold">{label}</h2>
      <p className="mt-2 text-2xl font-semibold tracking-tight">{value}</p>
    </section>
  );
}

function badgeLabel(type: StudentBadge["type"]): string {
  switch (type) {
    case "FirstModule":
      return t("dashboard.badge.FirstModule");
    case "FiveConsecutiveDays":
      return t("dashboard.badge.FiveConsecutiveDays");
    case "SubjectMastery":
      return t("dashboard.badge.SubjectMastery");
    case "GradeCompletion":
      return t("dashboard.badge.GradeCompletion");
  }
}
