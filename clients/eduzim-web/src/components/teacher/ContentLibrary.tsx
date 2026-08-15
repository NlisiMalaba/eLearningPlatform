"use client";

import { useState } from "react";
import { t } from "@/lib/i18n/t";
import { publishContent, type ContentLibraryItem } from "@/lib/teacher/teacherContentService";

type ContentLibraryProps = {
  items: ContentLibraryItem[];
  onChanged: () => void;
};

export function ContentLibrary({ items, onChanged }: ContentLibraryProps) {
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState(false);

  async function publish(id: string): Promise<void> {
    setBusyId(id);
    setError(false);
    try {
      await publishContent(id);
      onChanged();
    } catch {
      setError(true);
    } finally {
      setBusyId(null);
    }
  }

  if (items.length === 0) {
    return <p className="text-sm text-zinc-600">{t("teacher.library.empty")}</p>;
  }

  return (
    <div className="flex flex-col gap-3">
      {error ? (
        <p role="alert" className="text-sm text-red-700">
          {t("teacher.publish.failed")}
        </p>
      ) : null}
      <ul className="divide-y divide-zinc-200 rounded-xl border border-zinc-200 bg-white">
        {items.map((item) => (
          <li key={item.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
            <div>
              <p className="font-medium">{item.title}</p>
              <p className="text-sm text-zinc-600">
                {item.type} · {item.status === "Published" ? t("teacher.status.published") : t("teacher.status.draft")}
              </p>
            </div>
            {item.status === "Draft" ? (
              <button
                type="button"
                disabled={busyId === item.id}
                onClick={() => void publish(item.id)}
                className="rounded-lg border border-[#0B6E4F] px-3 py-1.5 text-sm font-medium text-[#0B6E4F] disabled:opacity-60"
              >
                {t("teacher.publish.tenant")}
              </button>
            ) : null}
          </li>
        ))}
      </ul>
    </div>
  );
}
