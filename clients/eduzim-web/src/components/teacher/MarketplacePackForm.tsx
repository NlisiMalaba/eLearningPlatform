"use client";

import { useState, type FormEvent } from "react";
import { t } from "@/lib/i18n/t";
import { submitMarketplacePack } from "@/lib/teacher/teacherContentService";

type MarketplacePackFormProps = {
  contentItemIds: string[];
};

export function MarketplacePackForm({ contentItemIds }: MarketplacePackFormProps) {
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState<"idle" | "ok" | "error">("idle");

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const form = event.currentTarget;
    const titleField = form.elements.namedItem("packTitle");
    const descriptionField = form.elements.namedItem("packDescription");
    const title = titleField instanceof HTMLInputElement ? titleField.value.trim() : "";
    const description =
      descriptionField instanceof HTMLTextAreaElement ? descriptionField.value.trim() : "";
    if (!title || !description || contentItemIds.length === 0) {
      setStatus("error");
      return;
    }

    setBusy(true);
    try {
      await submitMarketplacePack({ title, description, contentItemIds });
      form.reset();
      setStatus("ok");
    } catch {
      setStatus("error");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4 rounded-xl border border-zinc-200 bg-white p-4">
      <h2 className="text-lg font-semibold">{t("teacher.marketplace.title")}</h2>
      <p className="text-sm text-zinc-600">{t("teacher.marketplace.help")}</p>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.marketplace.name")}
        <input name="packTitle" required className="rounded-lg border border-zinc-300 px-3 py-2" />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("teacher.marketplace.description")}
        <textarea name="packDescription" required rows={3} className="rounded-lg border border-zinc-300 px-3 py-2" />
      </label>
      {status === "ok" ? <p role="status">{t("teacher.marketplace.submitted")}</p> : null}
      {status === "error" ? (
        <p role="alert" className="text-sm text-red-700">
          {t("teacher.marketplace.failed")}
        </p>
      ) : null}
      <button
        type="submit"
        disabled={busy || contentItemIds.length === 0}
        className="self-start rounded-lg border border-[#0B6E4F] px-4 py-2 text-sm font-semibold text-[#0B6E4F] disabled:opacity-60"
      >
        {t("teacher.marketplace.submit")}
      </button>
    </form>
  );
}
