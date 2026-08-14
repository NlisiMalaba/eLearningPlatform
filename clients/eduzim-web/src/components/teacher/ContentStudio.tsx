"use client";

import { useQuery } from "@tanstack/react-query";
import { ContentLibrary } from "@/components/teacher/ContentLibrary";
import { ContentUploader } from "@/components/teacher/ContentUploader";
import { t } from "@/lib/i18n/t";
import { listContent } from "@/lib/teacher/teacherContentService";

export function ContentStudio() {
  const query = useQuery({
    queryKey: ["teacher-content"],
    queryFn: listContent,
  });

  return (
    <div className="flex flex-col gap-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t("teacher.content.title")}</h1>
        <p className="mt-2 max-w-xl text-zinc-600">{t("teacher.content.subtitle")}</p>
      </header>
      <ContentUploader onUploaded={() => void query.refetch()} />
      <section className="flex flex-col gap-3">
        <h2 className="text-lg font-semibold">{t("teacher.library.title")}</h2>
        {query.isError ? (
          <p role="alert">{t("teacher.content.error")}</p>
        ) : query.isLoading ? (
          <p>{t("teacher.content.loading")}</p>
        ) : (
          <ContentLibrary items={query.data ?? []} onChanged={() => void query.refetch()} />
        )}
      </section>
    </div>
  );
}
