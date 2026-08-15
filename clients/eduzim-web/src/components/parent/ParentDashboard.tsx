"use client";

import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ActivityFeed } from "@/components/dashboard/ActivityFeed";
import { ScreenTimeLimitForm } from "@/components/parent/ScreenTimeLimitForm";
import type { LinkedStudentDashboard, ParentDashboardData } from "@/lib/parent/types";
import { fetchParentDashboard, setDailyScreenTimeLimit } from "@/lib/parent/parentDashboardService";
import { GRADE_MESSAGE_KEYS } from "@/lib/teacher/gradeMessages";
import type { StudentBadge } from "@/lib/dashboard/types";
import { t } from "@/lib/i18n/t";

type ParentDashboardProps = {
  parentId: string;
  tenantId?: string;
  initialData: ParentDashboardData | null;
};

export function ParentDashboard({ parentId, tenantId, initialData }: ParentDashboardProps) {
  const [savingStudentId, setSavingStudentId] = useState<string | null>(null);
  const query = useQuery({
    queryKey: ["parent-dashboard", parentId, tenantId ?? ""],
    queryFn: () => fetchParentDashboard(parentId, tenantId),
    initialData: initialData ?? undefined,
    refetchInterval: 15_000,
    refetchOnWindowFocus: true,
  });

  if (query.isLoading && !query.data) {
    return <p>{t("parent.loading")}</p>;
  }

  if (query.isError || !query.data) {
    return <p role="alert">{t("parent.error")}</p>;
  }

  async function saveLimit(studentId: string, seconds: number | null): Promise<void> {
    setSavingStudentId(studentId);
    try {
      await setDailyScreenTimeLimit(studentId, seconds, tenantId);
      await query.refetch();
    } finally {
      setSavingStudentId(null);
    }
  }

  const data = query.data;

  return (
    <div className="flex flex-col gap-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t("parent.title")}</h1>
        <p className="mt-2 max-w-xl text-zinc-600">{t("parent.subtitle")}</p>
      </header>
      {data.students.length === 0 ? (
        <p className="text-sm text-zinc-600">{t("parent.empty")}</p>
      ) : (
        <div className="flex flex-col gap-8">
          {data.students.map((student) => (
            <LinkedStudentCard
              key={student.studentId}
              student={student}
              busy={savingStudentId === student.studentId}
              onSaveLimit={saveLimit}
            />
          ))}
        </div>
      )}
    </div>
  );
}

function LinkedStudentCard({
  student,
  busy,
  onSaveLimit,
}: {
  student: LinkedStudentDashboard;
  busy: boolean;
  onSaveLimit: (studentId: string, seconds: number | null) => Promise<void>;
}) {
  const name = student.displayName || t("parent.student.unnamed");
  const summary = student.weeklySummary;

  return (
    <article className="flex flex-col gap-6 rounded-2xl border border-zinc-200 bg-zinc-50 p-5">
      <header className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">{name}</h2>
          <p className="mt-1 text-sm text-zinc-600">
            {t("parent.grade")}: {t(GRADE_MESSAGE_KEYS[student.currentGrade])}
          </p>
        </div>
        <p className="text-2xl font-semibold text-[#0B6E4F]">
          {t("dashboard.percent").replace("{value}", String(student.overallProgressPercent))}
        </p>
      </header>
      <section>
        <h3 className="mb-2 text-sm font-semibold">{t("parent.subjects")}</h3>
        {student.subjects.length === 0 ? (
          <p className="text-sm text-zinc-600">{t("parent.subjects.empty")}</p>
        ) : (
          <ul className="flex flex-wrap gap-2">
            {student.subjects.map((subject) => (
              <li key={subject} className="rounded-full border border-zinc-300 bg-white px-3 py-1 text-sm">
                {subject}
              </li>
            ))}
          </ul>
        )}
      </section>
      <section>
        <h3 className="mb-2 text-sm font-semibold">{t("dashboard.badges")}</h3>
        <BadgeList badges={student.badges} />
      </section>
      <section>
        <h3 className="mb-2 text-sm font-semibold">{t("parent.weekly.title")}</h3>
        {summary ? (
          <dl className="grid gap-3 sm:grid-cols-2">
            <SummaryStat label={t("parent.weekly.modules")} value={String(summary.modulesMarkedCompleteInWeek)} />
            <SummaryStat label={t("parent.weekly.badges")} value={String(summary.badgesEarnedInWeek)} />
            <SummaryStat
              label={t("parent.weekly.assessments")}
              value={String(summary.assessmentsSubmittedCount)}
            />
            <SummaryStat
              label={t("parent.weekly.average")}
              value={
                summary.averageScorePercent === null
                  ? t("parent.weekly.none")
                  : t("dashboard.percent").replace("{value}", String(summary.averageScorePercent))
              }
            />
          </dl>
        ) : (
          <p className="text-sm text-zinc-600">{t("parent.weekly.empty")}</p>
        )}
      </section>
      <section>
        <h3 className="mb-2 text-sm font-semibold">{t("dashboard.activity")}</h3>
        <ActivityFeed items={student.recentActivity} />
      </section>
      <ScreenTimeLimitForm
        studentId={student.studentId}
        dailyScreenTimeLimitSeconds={student.dailyScreenTimeLimitSeconds}
        busy={busy}
        onSave={onSaveLimit}
      />
    </article>
  );
}

function BadgeList({ badges }: { badges: readonly StudentBadge[] }) {
  if (badges.length === 0) {
    return <p className="text-sm text-zinc-600">{t("dashboard.badges.empty")}</p>;
  }

  return (
    <ul className="flex flex-wrap gap-2">
      {badges.map((badge) => (
        <li
          key={badge.badgeId}
          className="rounded-full border border-[#0B6E4F] px-3 py-1 text-sm text-[#0B6E4F]"
        >
          {badgeLabel(badge.type)}
        </li>
      ))}
    </ul>
  );
}

function SummaryStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-zinc-200 bg-white px-3 py-2">
      <dt className="text-xs text-zinc-600">{label}</dt>
      <dd className="mt-1 text-lg font-semibold">{value}</dd>
    </div>
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
