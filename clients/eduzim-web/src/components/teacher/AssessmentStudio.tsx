"use client";

import { useQuery } from "@tanstack/react-query";
import { AssessmentBuilder } from "@/components/teacher/AssessmentBuilder";
import { t } from "@/lib/i18n/t";
import { listAssessments } from "@/lib/teacher/assessmentService";

export function AssessmentStudio() {
  const query = useQuery({
    queryKey: ["teacher-assessments"],
    queryFn: listAssessments,
  });

  return (
    <div className="flex flex-col gap-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t("teacher.assessment.studioTitle")}</h1>
        <p className="mt-2 max-w-xl text-zinc-600">{t("teacher.assessment.studioSubtitle")}</p>
      </header>
      <section>
        <h2 className="mb-3 text-lg font-semibold">{t("teacher.assessment.list")}</h2>
        {query.isError ? (
          <p role="alert">{t("teacher.assessment.listError")}</p>
        ) : query.isLoading ? (
          <p>{t("teacher.assessment.listLoading")}</p>
        ) : (query.data ?? []).length === 0 ? (
          <p className="text-sm text-zinc-600">{t("teacher.assessment.listEmpty")}</p>
        ) : (
          <ul className="divide-y divide-zinc-200 rounded-xl border border-zinc-200 bg-white">
            {(query.data ?? []).map((item) => (
              <li key={item.id}>
                <a
                  href={`/teacher/assessments/${item.id}`}
                  className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 hover:bg-zinc-50"
                >
                  <span className="font-medium">{item.title}</span>
                  <span className="text-sm text-zinc-600">
                    {item.questionCount} · {item.passingScorePercent}%
                    {item.timeLimitSeconds
                      ? ` · ${Math.round(item.timeLimitSeconds / 60)} ${t("teacher.assessment.min")}`
                      : ""}
                  </span>
                </a>
              </li>
            ))}
          </ul>
        )}
      </section>
      <AssessmentBuilder />
    </div>
  );
}
