"use client";

import { useEffect, useState, type FormEvent } from "react";
import { t } from "@/lib/i18n/t";
import type { ContentLibraryItem } from "@/lib/teacher/teacherContentService";
import type { StudentMediaState } from "@/lib/classroom/types";

type TeacherClassroomControlsProps = {
  currentUserId?: string;
  sharing: boolean;
  participantIds: readonly string[];
  studentMedia: Record<string, StudentMediaState>;
  contentItems: readonly ContentLibraryItem[];
  onStartShare: () => Promise<void>;
  onStopShare: () => Promise<void>;
  onPresent: (contentItemId: string) => Promise<void>;
  onSetAudio: (studentUserId: string, enabled: boolean) => Promise<void>;
  onSetVideo: (studentUserId: string, enabled: boolean) => Promise<void>;
  onEnd: () => Promise<void>;
};

export function TeacherClassroomControls({
  currentUserId,
  sharing,
  participantIds,
  studentMedia,
  contentItems,
  onStartShare,
  onStopShare,
  onPresent,
  onSetAudio,
  onSetVideo,
  onEnd,
}: TeacherClassroomControlsProps) {
  const [contentItemId, setContentItemId] = useState(contentItems[0]?.id ?? "");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!contentItemId && contentItems[0]) {
      setContentItemId(contentItems[0].id);
    }
  }, [contentItemId, contentItems]);

  async function run(work: () => Promise<void>): Promise<void> {
    setBusy(true);
    try {
      await work();
    } finally {
      setBusy(false);
    }
  }

  async function onPresentSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    if (!contentItemId) {
      return;
    }

    await run(() => onPresent(contentItemId));
  }

  return (
    <section className="flex flex-col gap-4 rounded-xl border border-zinc-200 bg-white p-4">
      <h2 className="text-lg font-semibold">{t("classroom.controls.title")}</h2>
      <p className="text-sm text-zinc-600">{t("classroom.controls.help")}</p>
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          disabled={busy}
          onClick={() => run(sharing ? onStopShare : onStartShare)}
          className="rounded-lg bg-[#0B6E4F] px-3 py-2 text-sm font-semibold text-white disabled:opacity-60"
        >
          {sharing ? t("classroom.controls.stopShare") : t("classroom.controls.startShare")}
        </button>
        <button
          type="button"
          disabled={busy}
          onClick={() => run(onEnd)}
          className="rounded-lg border border-red-700 px-3 py-2 text-sm font-semibold text-red-700 disabled:opacity-60"
        >
          {t("classroom.controls.end")}
        </button>
      </div>
      <form onSubmit={onPresentSubmit} className="flex flex-col gap-2 sm:flex-row sm:items-end">
        <label className="flex flex-1 flex-col gap-1 text-sm">
          {t("classroom.controls.content")}
          <select
            className="rounded-lg border border-zinc-300 px-3 py-2"
            value={contentItemId}
            onChange={(event) => setContentItemId(event.target.value)}
          >
            {contentItems.length === 0 ? (
              <option value="">{t("classroom.controls.contentEmpty")}</option>
            ) : null}
            {contentItems.map((item) => (
              <option key={item.id} value={item.id}>
                {item.title}
              </option>
            ))}
          </select>
        </label>
        <button
          type="submit"
          disabled={busy || !contentItemId}
          className="rounded-lg border border-[#0B6E4F] px-3 py-2 text-sm font-semibold text-[#0B6E4F] disabled:opacity-60"
        >
          {t("classroom.controls.present")}
        </button>
      </form>
      <ul className="flex flex-col gap-2">
        {participantIds
          .filter((id) => id !== currentUserId)
          .map((userId) => {
            const media = studentMedia[userId] ?? {
              studentUserId: userId,
              audioEnabled: true,
              videoEnabled: true,
            };
            return (
              <li
                key={userId}
                className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-zinc-200 px-3 py-2 text-sm"
              >
                <span className="font-mono text-xs">{shortId(userId)}</span>
                <span className="flex gap-2">
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => run(() => onSetAudio(userId, !media.audioEnabled))}
                    className="underline"
                  >
                    {media.audioEnabled
                      ? t("classroom.controls.muteAudio")
                      : t("classroom.controls.unmuteAudio")}
                  </button>
                  <button
                    type="button"
                    disabled={busy}
                    onClick={() => run(() => onSetVideo(userId, !media.videoEnabled))}
                    className="underline"
                  >
                    {media.videoEnabled
                      ? t("classroom.controls.muteVideo")
                      : t("classroom.controls.unmuteVideo")}
                  </button>
                </span>
              </li>
            );
          })}
      </ul>
    </section>
  );
}

function shortId(id: string): string {
  return id.slice(0, 8);
}
