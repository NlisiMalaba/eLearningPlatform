import { t } from "@/lib/i18n/t";
import type { SubjectProgress } from "@/lib/dashboard/types";

type SubjectProgressListProps = {
  subjects: readonly SubjectProgress[];
};

export function SubjectProgressList({ subjects }: SubjectProgressListProps) {
  if (subjects.length === 0) {
    return <p className="text-sm text-zinc-600">{t("dashboard.subjects.empty")}</p>;
  }

  return (
    <ul className="flex flex-col gap-4">
      {subjects.map((subject) => (
        <li key={subject.subject}>
          <div className="mb-1 flex items-center justify-between gap-3 text-sm">
            <span className="font-medium">{subject.subject}</span>
            <span>{t("dashboard.percent").replace("{value}", String(subject.progressPercent))}</span>
          </div>
          <div
            role="progressbar"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={subject.progressPercent}
            aria-label={subject.subject}
            className="h-3 overflow-hidden rounded-full bg-zinc-200"
          >
            <div
              className="h-full rounded-full bg-[#0B6E4F]"
              style={{ width: `${subject.progressPercent}%` }}
            />
          </div>
        </li>
      ))}
    </ul>
  );
}
