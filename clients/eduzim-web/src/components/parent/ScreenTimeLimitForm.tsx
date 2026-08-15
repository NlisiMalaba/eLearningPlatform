"use client";

import { useState, type FormEvent } from "react";
import { DEFAULT_LIMIT_HOURS, parseHoursInput, secondsToHours, toLimitSeconds } from "@/lib/parent/screenTime";
import { t } from "@/lib/i18n/t";

type ScreenTimeLimitFormProps = {
  studentId: string;
  dailyScreenTimeLimitSeconds: number | null;
  busy: boolean;
  onSave: (studentId: string, seconds: number | null) => Promise<void>;
};

export function ScreenTimeLimitForm({
  studentId,
  dailyScreenTimeLimitSeconds,
  busy,
  onSave,
}: ScreenTimeLimitFormProps) {
  const initialHours = secondsToHours(dailyScreenTimeLimitSeconds);
  const [enabled, setEnabled] = useState(initialHours !== null);
  const [hours, setHours] = useState(String(initialHours ?? DEFAULT_LIMIT_HOURS));
  const [status, setStatus] = useState<"idle" | "saved" | "invalid" | "failed">("idle");

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const parsedHours = parseHoursInput(hours);
    const next = toLimitSeconds(enabled, parsedHours);
    if (next === "invalid") {
      setStatus("invalid");
      return;
    }

    setStatus("idle");
    try {
      await onSave(studentId, next);
      setStatus("saved");
    } catch {
      setStatus("failed");
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-3 rounded-xl border border-zinc-200 bg-white p-4">
      <h3 className="text-sm font-semibold">{t("parent.screenTime.title")}</h3>
      <p className="text-sm text-zinc-600">{t("parent.screenTime.help")}</p>
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={enabled}
          onChange={(event) => {
            setEnabled(event.target.checked);
            setStatus("idle");
          }}
        />
        {t("parent.screenTime.enable")}
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("parent.screenTime.hours")}
        <input
          type="number"
          min={0.25}
          max={24}
          step={0.25}
          value={hours}
          disabled={!enabled || busy}
          onChange={(event) => {
            setHours(event.target.value);
            setStatus("idle");
          }}
          className="rounded-md border border-zinc-300 px-3 py-2"
        />
      </label>
      <button
        type="submit"
        disabled={busy}
        className="self-start rounded-md bg-[#0B6E4F] px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
      >
        {t("parent.screenTime.submit")}
      </button>
      {status === "saved" ? <p className="text-sm text-[#0B6E4F]">{t("parent.screenTime.saved")}</p> : null}
      {status === "invalid" ? (
        <p role="alert" className="text-sm text-red-700">
          {t("parent.screenTime.invalid")}
        </p>
      ) : null}
      {status === "failed" ? (
        <p role="alert" className="text-sm text-red-700">
          {t("parent.screenTime.failed")}
        </p>
      ) : null}
    </form>
  );
}
