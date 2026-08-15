"use client";

import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { MarketplacePackForm } from "@/components/teacher/MarketplacePackForm";
import { getModule } from "@/lib/content/contentService";
import { t } from "@/lib/i18n/t";
import {
  attachedRowTitle,
  moveAttachedId,
  publishDraftItems,
  saveModuleSequence,
  toggleAttachedId,
} from "@/lib/teacher/moduleBuilderActions";
import { listContent, type ContentLibraryItem } from "@/lib/teacher/teacherContentService";

type ModuleBuilderProps = {
  moduleId: string;
};

export function ModuleBuilder({ moduleId }: ModuleBuilderProps) {
  const moduleQuery = useQuery({
    queryKey: ["teacher-module", moduleId],
    queryFn: () => getModule(moduleId),
  });
  const libraryQuery = useQuery({
    queryKey: ["teacher-content"],
    queryFn: listContent,
  });
  const [draftIds, setDraftIds] = useState<string[] | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);

  const attachedIds = useMemo(() => {
    if (draftIds) {
      return draftIds;
    }

    return (moduleQuery.data?.contentItems ?? [])
      .slice()
      .sort((a, b) => a.sequenceOrder - b.sequenceOrder)
      .map((item) => item.contentItemId);
  }, [draftIds, moduleQuery.data]);

  if (moduleQuery.isLoading || libraryQuery.isLoading) {
    return <p>{t("teacher.builder.loading")}</p>;
  }

  if (moduleQuery.isError || libraryQuery.isError || !moduleQuery.data) {
    return <p role="alert">{t("teacher.builder.error")}</p>;
  }

  const library = libraryQuery.data ?? [];
  const module = moduleQuery.data;

  return (
    <div className="flex flex-col gap-8">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{module.title}</h1>
        <p className="mt-2 text-zinc-600">
          {module.subject} · {t("teacher.builder.reorderHelp")}
        </p>
      </header>
      {error ? (
        <p role="alert" className="text-sm text-red-700">
          {t("teacher.builder.saveFailed")}
        </p>
      ) : null}
      <ItemPicker
        library={library}
        attachedIds={attachedIds}
        onToggle={(id) => setDraftIds(toggleAttachedId(attachedIds, id))}
      />
      <OrderedList
        items={attachedIds.flatMap((id) => {
          const title = attachedRowTitle(id, library, module.contentItems);
          return title ? [{ id, title }] : [];
        })}
        onMove={(id, direction) => setDraftIds(moveAttachedId(attachedIds, id, direction))}
      />
      <BuilderActions
        busy={busy}
        onSave={() =>
          void runAction(setBusy, setError, async () => {
            await saveModuleSequence(moduleId, attachedIds);
            setDraftIds(null);
            await moduleQuery.refetch();
          })
        }
        onPublish={() =>
          void runAction(setBusy, setError, async () => {
            await publishDraftItems(library, attachedIds);
            await libraryQuery.refetch();
          })
        }
      />
      <MarketplacePackForm contentItemIds={attachedIds} />
    </div>
  );
}

function ItemPicker({
  library,
  attachedIds,
  onToggle,
}: {
  library: ContentLibraryItem[];
  attachedIds: string[];
  onToggle: (id: string) => void;
}) {
  return (
    <section>
      <h2 className="mb-3 text-lg font-semibold">{t("teacher.builder.library")}</h2>
      <ul className="grid gap-2">
        {library.map((item) => (
          <li key={item.id}>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={attachedIds.includes(item.id)}
                onChange={() => onToggle(item.id)}
              />
              {item.title}
            </label>
          </li>
        ))}
      </ul>
    </section>
  );
}

function OrderedList({
  items,
  onMove,
}: {
  items: { id: string; title: string }[];
  onMove: (id: string, direction: -1 | 1) => void;
}) {
  return (
    <section>
      <h2 className="mb-3 text-lg font-semibold">{t("teacher.builder.order")}</h2>
      {items.length === 0 ? (
        <p className="text-sm text-zinc-600">{t("teacher.builder.empty")}</p>
      ) : (
        <ol className="flex flex-col gap-2">
          {items.map((item, index) => (
            <li
              key={item.id}
              className="flex items-center justify-between rounded-lg border border-zinc-200 bg-white px-3 py-2"
            >
              <span>
                {index + 1}. {item.title}
              </span>
              <span className="flex gap-2">
                <button type="button" onClick={() => onMove(item.id, -1)} aria-label={t("teacher.builder.up")}>
                  ↑
                </button>
                <button type="button" onClick={() => onMove(item.id, 1)} aria-label={t("teacher.builder.down")}>
                  ↓
                </button>
              </span>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}

function BuilderActions({
  busy,
  onSave,
  onPublish,
}: {
  busy: boolean;
  onSave: () => void;
  onPublish: () => void;
}) {
  return (
    <div className="flex flex-wrap gap-3">
      <button
        type="button"
        disabled={busy}
        onClick={onSave}
        className="rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
      >
        {t("teacher.builder.save")}
      </button>
      <button
        type="button"
        disabled={busy}
        onClick={onPublish}
        className="rounded-lg border border-[#0B6E4F] px-4 py-2 text-sm font-semibold text-[#0B6E4F] disabled:opacity-60"
      >
        {t("teacher.publish.tenant")}
      </button>
    </div>
  );
}

async function runAction(
  setBusy: (value: boolean) => void,
  setError: (value: boolean) => void,
  action: () => Promise<void>,
): Promise<void> {
  setBusy(true);
  setError(false);
  try {
    await action();
  } catch {
    setError(true);
  } finally {
    setBusy(false);
  }
}
