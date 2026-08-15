"use client";

import { useCallback, useState, type DragEvent, type FormEvent } from "react";
import { t } from "@/lib/i18n/t";
import type { MessageKey } from "@/lib/i18n/messages";
import { uploadContent } from "@/lib/teacher/teacherContentService";
import {
  inferUploadType,
  UPLOADABLE_TYPES,
  validateUploadFile,
  type UploadableType,
  type UploadValidationError,
} from "@/lib/teacher/uploadValidation";

type ContentUploaderProps = {
  onUploaded: () => void;
};

const UPLOAD_ERROR_KEYS: Record<UploadValidationError, MessageKey> = {
  type: "teacher.upload.typeMismatch",
  size: "teacher.upload.size",
  empty: "teacher.upload.empty",
};

export function ContentUploader({ onUploaded }: ContentUploaderProps) {
  const [errorKey, setErrorKey] = useState<MessageKey | null>(null);
  const [busy, setBusy] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [type, setType] = useState<UploadableType>("Pdf");

  const applyFile = useCallback((next: File | null) => {
    setFile(next);
    if (!next) {
      return;
    }

    const inferred = inferUploadType(next);
    if (inferred) {
      setType(inferred);
    }
  }, []);

  function onDrop(event: DragEvent<HTMLLabelElement>): void {
    event.preventDefault();
    applyFile(event.dataTransfer.files[0] ?? null);
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const titleInput = event.currentTarget.elements.namedItem("title");
    const title = titleInput instanceof HTMLInputElement ? titleInput.value.trim() : "";
    if (!file || !title) {
      setErrorKey("teacher.upload.missing");
      return;
    }

    const invalid = validateUploadFile(file, type);
    if (invalid) {
      setErrorKey(UPLOAD_ERROR_KEYS[invalid]);
      return;
    }

    setBusy(true);
    setErrorKey(null);
    try {
      await uploadContent({ title, type, file });
      event.currentTarget.reset();
      setFile(null);
      onUploaded();
    } catch {
      setErrorKey("teacher.upload.failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4 rounded-xl border border-zinc-200 bg-white p-4">
      <h2 className="text-lg font-semibold">{t("teacher.upload.heading")}</h2>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.upload.name")}
        <input name="title" required className="rounded-lg border border-zinc-300 px-3 py-2" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.upload.type")}
        <select
          className="rounded-lg border border-zinc-300 px-3 py-2"
          value={type}
          onChange={(event) => setType(event.target.value as UploadableType)}
        >
          {UPLOADABLE_TYPES.map((item) => (
            <option key={item} value={item}>
              {item}
            </option>
          ))}
        </select>
      </label>
      <label
        onDragOver={(event) => event.preventDefault()}
        onDrop={onDrop}
        className="flex cursor-pointer flex-col items-center rounded-lg border-2 border-dashed border-zinc-300 px-4 py-8 text-center text-sm"
      >
        <input
          type="file"
          className="sr-only"
          accept=".pdf,.mp4,.mp3,.wav,application/pdf,video/mp4,audio/mpeg,audio/wav"
          onChange={(event) => applyFile(event.target.files?.[0] ?? null)}
        />
        <span>{t("teacher.upload.drop")}</span>
        {file ? <span className="mt-2 font-medium">{file.name}</span> : null}
      </label>
      {errorKey ? (
        <p role="alert" className="text-sm text-red-700">
          {t(errorKey)}
        </p>
      ) : (
        <p className="text-xs text-zinc-600">{t("teacher.upload.limits")}</p>
      )}
      <button
        type="submit"
        disabled={busy}
        className="self-start rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
      >
        {t("teacher.upload.submit")}
      </button>
    </form>
  );
}
