import { t } from "@/lib/i18n/t";
import type { ClassResults } from "@/lib/teacher/assessmentService";

type ClassResultsTableProps = {
  results: ClassResults;
};

export function ClassResultsTable({ results }: ClassResultsTableProps) {
  return (
    <section className="flex flex-col gap-4">
      <h2 className="text-lg font-semibold">{t("teacher.results.title")}</h2>
      <p className="text-sm text-zinc-600">
        {results.schoolClassName} · {t("teacher.results.completion")}: {results.completionRatePercent}% (
        {results.completedCount}/{results.enrolledStudentCount}) · {t("teacher.results.avgTime")}:{" "}
        {formatSeconds(results.averageTimeTakenSecondsAmongCompleters)}
      </p>
      <div className="overflow-x-auto rounded-xl border border-zinc-200 bg-white">
        <table className="min-w-full text-left text-sm">
          <thead className="border-b border-zinc-200 bg-zinc-50">
            <tr>
              <th className="px-4 py-2 font-semibold">{t("teacher.results.student")}</th>
              <th className="px-4 py-2 font-semibold">{t("teacher.results.score")}</th>
              <th className="px-4 py-2 font-semibold">{t("teacher.results.status")}</th>
              <th className="px-4 py-2 font-semibold">{t("teacher.results.time")}</th>
            </tr>
          </thead>
          <tbody>
            {results.students.length === 0 ? (
              <tr>
                <td colSpan={4} className="px-4 py-3 text-zinc-600">
                  {t("teacher.results.empty")}
                </td>
              </tr>
            ) : (
              results.students.map((row) => (
                <tr key={row.studentId} className="border-t border-zinc-100">
                  <td className="px-4 py-2 font-mono text-xs">{row.studentId}</td>
                  <td className="px-4 py-2">
                    {row.scorePercent === null ? t("teacher.results.dash") : `${row.scorePercent}%`}
                  </td>
                  <td className="px-4 py-2">
                    {row.hasCompleted ? t("teacher.results.completed") : t("teacher.results.pending")}
                  </td>
                  <td className="px-4 py-2">{formatSeconds(row.timeTakenSeconds)}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

function formatSeconds(value: number | null): string {
  if (value === null) {
    return t("teacher.results.dash");
  }

  const minutes = Math.floor(value / 60);
  const seconds = value % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}
