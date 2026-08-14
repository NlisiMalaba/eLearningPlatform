"use client";

import { useQuery } from "@tanstack/react-query";
import { ActivityFeed } from "@/components/dashboard/ActivityFeed";
import { GamificationSummary } from "@/components/dashboard/GamificationSummary";
import { SubjectProgressList } from "@/components/dashboard/SubjectProgressList";
import { fetchStudentDashboard } from "@/lib/dashboard/dashboardService";
import type { StudentDashboardData } from "@/lib/dashboard/types";
import { t } from "@/lib/i18n/t";

type StudentDashboardProps = {
  studentId: string;
  tenantId?: string;
  initialData: StudentDashboardData | null;
};

export function StudentDashboard({ studentId, tenantId, initialData }: StudentDashboardProps) {
  const query = useQuery({
    queryKey: ["student-dashboard", studentId, tenantId ?? ""],
    queryFn: () => fetchStudentDashboard(studentId, tenantId),
    initialData: initialData ?? undefined,
    refetchInterval: 15_000,
    refetchOnWindowFocus: true,
  });

  if (query.isLoading && !query.data) {
    return <p>{t("dashboard.loading")}</p>;
  }

  if (query.isError || !query.data) {
    return <p role="alert">{t("dashboard.error")}</p>;
  }

  const data = query.data;

  return (
    <div className="flex flex-col gap-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t("dashboard.title")}</h1>
        <p className="mt-2 max-w-xl text-zinc-600">{t("dashboard.subtitle")}</p>
      </header>
      <GamificationSummary
        totalPoints={data.totalPoints}
        badges={data.badges}
        leaderboardRank={data.leaderboardRank}
      />
      <section>
        <h2 className="mb-3 text-lg font-semibold">{t("dashboard.subjects")}</h2>
        <SubjectProgressList subjects={data.subjects} />
      </section>
      {data.continueModule ? (
        <p>
          <a
            href={`/modules/${data.continueModule.moduleId}`}
            className="font-medium text-[#0B6E4F] underline"
          >
            {t("dashboard.continue").replace("{title}", data.continueModule.title)}
          </a>
        </p>
      ) : null}
      <section>
        <h2 className="mb-3 text-lg font-semibold">{t("dashboard.activity")}</h2>
        <ActivityFeed items={data.recentActivity} />
      </section>
    </div>
  );
}
