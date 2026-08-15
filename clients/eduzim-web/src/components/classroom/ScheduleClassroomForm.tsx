"use client";

import { useEffect, useState, type FormEvent } from "react";
import { scheduleClassroom } from "@/lib/classroom/classroomService";
import { t } from "@/lib/i18n/t";
import { listSchoolClasses, type SchoolClassItem } from "@/lib/teacher/assessmentService";

export function ScheduleClassroomForm() {
  const [classes, setClasses] = useState<SchoolClassItem[]>([]);
  const [schoolClassId, setSchoolClassId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void listSchoolClasses()
      .then((items) => {
        if (!cancelled) {
          setClasses(items);
          setSchoolClassId(items[0]?.id ?? "");
        }
      })
      .catch(() => {
        if (!cancelled) {
          setError(true);
        }
      });
    return () => {
      cancelled = true;
    };
  }, []);

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const startField = event.currentTarget.elements.namedItem("startAt");
    const durationField = event.currentTarget.elements.namedItem("durationMinutes");
    const startValue = startField instanceof HTMLInputElement ? startField.value : "";
    const durationValue = durationField instanceof HTMLInputElement ? Number(durationField.value) : 0;
    const start = startValue ? new Date(startValue) : null;
    if (!schoolClassId || !start || Number.isNaN(start.getTime()) || durationValue < 1) {
      setError(true);
      return;
    }

    setBusy(true);
    setError(false);
    try {
      const sessionId = await scheduleClassroom({
        schoolClassId,
        startAtUtc: start.toISOString(),
        durationMinutes: durationValue,
      });
      window.location.href = `/classrooms/${sessionId}`;
    } catch {
      setError(true);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="mt-6 flex max-w-xl flex-col gap-4">
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.assessment.class")}
        <select
          className="rounded-lg border border-zinc-300 px-3 py-2"
          value={schoolClassId}
          onChange={(event) => setSchoolClassId(event.target.value)}
        >
          {classes.length === 0 ? <option value="">{t("teacher.assessment.noClasses")}</option> : null}
          {classes.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("classroom.schedule.start")}
        <input name="startAt" type="datetime-local" required className="rounded-lg border border-zinc-300 px-3 py-2" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("classroom.schedule.duration")}
        <input
          name="durationMinutes"
          type="number"
          min={1}
          max={480}
          defaultValue={45}
          required
          className="rounded-lg border border-zinc-300 px-3 py-2"
        />
      </label>
      {error ? (
        <p role="alert" className="text-sm text-red-700">
          {t("classroom.schedule.failed")}
        </p>
      ) : null}
      <button
        type="submit"
        disabled={busy || classes.length === 0}
        className="self-start rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
      >
        {t("classroom.schedule.submit")}
      </button>
    </form>
  );
}
