"use client";

import { useQuery } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { ContentItemPlayer } from "@/components/learning/ContentItemPlayer";
import { getModule } from "@/lib/content/contentService";
import type { ModuleContentItem } from "@/lib/content/types";
import { t } from "@/lib/i18n/t";

type ModuleViewerProps = {
  moduleId: string;
  initialContentItemId?: string;
};

export function ModuleViewer({ moduleId, initialContentItemId }: ModuleViewerProps) {
  const moduleQuery = useQuery({
    queryKey: ["module", moduleId],
    queryFn: () => getModule(moduleId),
  });

  const items = moduleQuery.data?.contentItems ?? [];
  const [selectedId, setSelectedId] = useState<string | undefined>(initialContentItemId);
  const activeId = useMemo(
    () => resolveActiveItemId(items, selectedId),
    [items, selectedId],
  );

  if (moduleQuery.isLoading) {
    return <p>{t("module.loading")}</p>;
  }

  if (moduleQuery.isError || !moduleQuery.data) {
    return <p role="alert">{t("module.error")}</p>;
  }

  if (items.length === 0) {
    return (
      <article>
        <ModuleHeading title={moduleQuery.data.title} subject={moduleQuery.data.subject} />
        <p className="mt-4 text-zinc-600">{t("module.empty")}</p>
      </article>
    );
  }

  return (
    <article className="grid gap-8 lg:grid-cols-[16rem_1fr]">
      <nav aria-label={t("module.items")}>
        <ModuleHeading title={moduleQuery.data.title} subject={moduleQuery.data.subject} />
        <ol className="mt-4 flex flex-col gap-2">
          {items.map((item) => (
            <li key={item.contentItemId}>
              <button
                type="button"
                aria-current={item.contentItemId === activeId ? "true" : undefined}
                aria-label={`${t("module.item")}: ${item.title}`}
                onClick={() => setSelectedId(item.contentItemId)}
                className={`w-full rounded-lg border px-3 py-2 text-left text-sm ${
                  item.contentItemId === activeId
                    ? "border-[#0B6E4F] bg-[#0B6E4F]/10 font-semibold"
                    : "border-zinc-200 bg-white"
                }`}
              >
                {item.title}
              </button>
            </li>
          ))}
        </ol>
      </nav>
      <section>{activeId ? <ContentItemPlayer contentItemId={activeId} /> : null}</section>
    </article>
  );
}

function ModuleHeading({ title, subject }: { title: string; subject: string }) {
  return (
    <header>
      <p className="text-sm font-medium text-[#0B6E4F]">{subject}</p>
      <h1 className="mt-1 text-2xl font-semibold tracking-tight">{title}</h1>
    </header>
  );
}

function resolveActiveItemId(
  items: readonly ModuleContentItem[],
  selectedId: string | undefined,
): string | undefined {
  if (selectedId && items.some((item) => item.contentItemId === selectedId)) {
    return selectedId;
  }

  return items[0]?.contentItemId;
}
