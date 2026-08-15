"use client";

import { useState, type FormEvent } from "react";
import { t } from "@/lib/i18n/t";
import { GRADE_LEVELS, type GradeLevel } from "@/lib/teacher/grades";
import { GRADE_MESSAGE_KEYS } from "@/lib/teacher/gradeMessages";
import { createModule } from "@/lib/teacher/teacherContentService";

type CreateModuleFormProps = {
  onCreated: (moduleId: string) => void;
};

export function CreateModuleForm({ onCreated }: CreateModuleFormProps) {
  const [grade, setGrade] = useState<GradeLevel>("Grade1");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const form = event.currentTarget;
    const title = readInput(form, "title");
    const subject = readInput(form, "subject");
    const sequenceRaw = readInput(form, "sequenceOrder");
    if (!title || !subject) {
      setError(true);
      return;
    }

    setBusy(true);
    setError(false);
    try {
      const moduleId = await createModule({
        title,
        grade,
        subject,
        sequenceOrder: Number.parseInt(sequenceRaw, 10) || 1,
      });
      form.reset();
      onCreated(moduleId);
    } catch {
      setError(true);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4 rounded-xl border border-zinc-200 bg-white p-4">
      <h2 className="text-lg font-semibold">{t("teacher.module.create")}</h2>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.module.titleField")}
        <input name="title" required className="rounded-lg border border-zinc-300 px-3 py-2" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.module.subject")}
        <input name="subject" required className="rounded-lg border border-zinc-300 px-3 py-2" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.module.grade")}
        <select
          className="rounded-lg border border-zinc-300 px-3 py-2"
          value={grade}
          onChange={(event) => setGrade(event.target.value as GradeLevel)}
        >
          {GRADE_LEVELS.map((item) => (
            <option key={item} value={item}>
              {t(GRADE_MESSAGE_KEYS[item])}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.module.sequence")}
        <input
          name="sequenceOrder"
          type="number"
          min={1}
          defaultValue={1}
          className="rounded-lg border border-zinc-300 px-3 py-2"
        />
      </label>
      {error ? (
        <p role="alert" className="text-sm text-red-700">
          {t("teacher.module.createFailed")}
        </p>
      ) : null}
      <button
        type="submit"
        disabled={busy}
        className="self-start rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
      >
        {t("teacher.module.submit")}
      </button>
    </form>
  );
}

function readInput(form: HTMLFormElement, name: string): string {
  const field = form.elements.namedItem(name);
  return field instanceof HTMLInputElement ? field.value.trim() : "";
}
