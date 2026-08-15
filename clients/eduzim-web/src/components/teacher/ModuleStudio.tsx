"use client";

import { useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { CreateModuleForm } from "@/components/teacher/CreateModuleForm";
import { t } from "@/lib/i18n/t";
import { GRADE_MESSAGE_KEYS } from "@/lib/teacher/gradeMessages";
import { listModules } from "@/lib/teacher/teacherContentService";

export function ModuleStudio() {
  const router = useRouter();
  const query = useQuery({
    queryKey: ["teacher-modules"],
    queryFn: listModules,
  });

  return (
    <div className="flex flex-col gap-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t("teacher.modules.title")}</h1>
        <p className="mt-2 max-w-xl text-zinc-600">{t("teacher.modules.subtitle")}</p>
      </header>
      <CreateModuleForm
        onCreated={(moduleId) => {
          void query.refetch();
          router.push(`/teacher/modules/${moduleId}`);
        }}
      />
      <section className="flex flex-col gap-3">
        <h2 className="text-lg font-semibold">{t("teacher.modules.list")}</h2>
        {query.isError ? (
          <p role="alert">{t("teacher.modules.error")}</p>
        ) : query.isLoading ? (
          <p>{t("teacher.modules.loading")}</p>
        ) : (query.data ?? []).length === 0 ? (
          <p className="text-sm text-zinc-600">{t("teacher.modules.empty")}</p>
        ) : (
          <ul className="divide-y divide-zinc-200 rounded-xl border border-zinc-200 bg-white">
            {(query.data ?? []).map((item) => (
              <li key={item.id}>
                <a
                  href={`/teacher/modules/${item.id}`}
                  className="flex flex-wrap items-center justify-between gap-2 px-4 py-3 hover:bg-zinc-50"
                >
                  <span className="font-medium">{item.title}</span>
                  <span className="text-sm text-zinc-600">
                    {t(GRADE_MESSAGE_KEYS[item.grade])} · {item.subject} · {item.contentItemCount}
                  </span>
                </a>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
