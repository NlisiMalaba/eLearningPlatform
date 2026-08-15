"use client";

import { useState, type FormEvent } from "react";
import { isClassroomSessionId } from "@/lib/classroom/parse";
import { t } from "@/lib/i18n/t";

export function ClassroomJoinForm() {
  const [error, setError] = useState(false);

  function onSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    const field = event.currentTarget.elements.namedItem("sessionId");
    const value = field instanceof HTMLInputElement ? field.value.trim() : "";
    if (!isClassroomSessionId(value)) {
      setError(true);
      return;
    }

    window.location.href = `/classrooms/${value}`;
  }

  return (
    <form onSubmit={onSubmit} className="mt-6 flex max-w-xl flex-col gap-3">
      <label className="flex flex-col gap-1 text-sm">
        {t("classroom.join.sessionId")}
        <input
          name="sessionId"
          required
          className="rounded-lg border border-zinc-300 px-3 py-2 font-mono"
          placeholder="00000000-0000-0000-0000-000000000000"
        />
      </label>
      {error ? (
        <p role="alert" className="text-sm text-red-700">
          {t("classroom.join.invalid")}
        </p>
      ) : null}
      <button type="submit" className="self-start rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white">
        {t("classroom.join.submit")}
      </button>
    </form>
  );
}
