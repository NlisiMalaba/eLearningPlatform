"use client";

import { useEffect, useState, type FormEvent } from "react";
import { t } from "@/lib/i18n/t";
import { assignAssessment, type SchoolClassItem } from "@/lib/teacher/assessmentService";

type AssignAssessmentFormProps = {
  assessmentId: string;
  classes: SchoolClassItem[];
  onAssigned: (schoolClassId: string) => void;
};

export function AssignAssessmentForm({ assessmentId, classes, onAssigned }: AssignAssessmentFormProps) {
  const [schoolClassId, setSchoolClassId] = useState(classes[0]?.id ?? "");
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState<"idle" | "ok" | "error">("idle");

  useEffect(() => {
    if (!schoolClassId && classes[0]) {
      setSchoolClassId(classes[0].id);
    }
  }, [classes, schoolClassId]);

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const dueField = event.currentTarget.elements.namedItem("dueAt");
    const localValue = dueField instanceof HTMLInputElement ? dueField.value : "";
    const due = localValue ? new Date(localValue) : null;
    if (!schoolClassId || !due || Number.isNaN(due.getTime())) {
      setStatus("error");
      return;
    }

    setBusy(true);
    try {
      await assignAssessment(assessmentId, schoolClassId, due.toISOString());
      setStatus("ok");
      onAssigned(schoolClassId);
    } catch {
      setStatus("error");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4 rounded-xl border border-zinc-200 bg-white p-4">
      <h2 className="text-lg font-semibold">{t("teacher.assessment.assign")}</h2>
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
        {t("teacher.assessment.due")}
        <input name="dueAt" type="datetime-local" required className="rounded-lg border border-zinc-300 px-3 py-2" />
      </label>
      {status === "ok" ? <p role="status">{t("teacher.assessment.assigned")}</p> : null}
      {status === "error" ? (
        <p role="alert" className="text-sm text-red-700">
          {t("teacher.assessment.assignFailed")}
        </p>
      ) : null}
      <button
        type="submit"
        disabled={busy || classes.length === 0}
        className="self-start rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
      >
        {t("teacher.assessment.assignSubmit")}
      </button>
    </form>
  );
}
