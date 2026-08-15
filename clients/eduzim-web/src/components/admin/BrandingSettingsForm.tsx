"use client";

import { useState, type FormEvent } from "react";
import { useQuery } from "@tanstack/react-query";
import { isHexColour, validateLogoFile } from "@/lib/admin/branding";
import { getTenantDetails, updateBranding, uploadBrandingLogo } from "@/lib/admin/tenantService";
import type { MessageKey } from "@/lib/i18n/messages";
import { t } from "@/lib/i18n/t";

type BrandingSettingsFormProps = {
  tenantId: string;
};

export function BrandingSettingsForm({ tenantId }: BrandingSettingsFormProps) {
  const query = useQuery({
    queryKey: ["tenant-details", tenantId],
    queryFn: () => getTenantDetails(tenantId),
  });
  const [busy, setBusy] = useState(false);
  const [logoError, setLogoError] = useState<MessageKey | null>(null);
  const [status, setStatus] = useState<"idle" | "saved" | "failed">("idle");
  const [logoPreview, setLogoPreview] = useState<string | null>(null);

  if (query.isLoading) {
    return <p>{t("admin.branding.loading")}</p>;
  }

  if (query.isError || !query.data) {
    return <p role="alert">{t("admin.branding.error")}</p>;
  }

  const details = query.data;
  const previewUrl = logoPreview ?? details.logoUrl;

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    const form = event.currentTarget;
    const schoolName = readInput(form, "schoolName");
    const primaryColour = readInput(form, "primaryColour");
    if (!schoolName || !isHexColour(primaryColour)) {
      setStatus("failed");
      return;
    }

    setBusy(true);
    setStatus("idle");
    try {
      await updateBranding(tenantId, { schoolName, primaryColour });
      setStatus("saved");
      await query.refetch();
    } catch {
      setStatus("failed");
    } finally {
      setBusy(false);
    }
  }

  async function onLogoChange(file: File | null): Promise<void> {
    if (!file) {
      return;
    }

    const invalid = validateLogoFile(file);
    if (invalid) {
      setLogoError(
        invalid === "size"
          ? "admin.branding.logo.size"
          : invalid === "empty"
            ? "admin.branding.logo.empty"
            : "admin.branding.logo.type",
      );
      return;
    }

    setBusy(true);
    setLogoError(null);
    try {
      const url = await uploadBrandingLogo(tenantId, file);
      setLogoPreview(url);
    } catch {
      setLogoError("admin.branding.logo.failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex max-w-xl flex-col gap-6">
      <header>
        <h1 className="text-2xl font-semibold tracking-tight">{t("admin.branding.title")}</h1>
        <p className="mt-2 text-zinc-600">{t("admin.branding.subtitle")}</p>
      </header>
      <label className="flex flex-col gap-1 text-sm">
        {t("admin.branding.schoolName")}
        <input
          name="schoolName"
          required
          defaultValue={details.schoolName}
          className="rounded-lg border border-zinc-300 px-3 py-2"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm">
        {t("admin.branding.colour")}
        <input name="primaryColour" type="color" defaultValue={details.primaryColour} className="h-10 w-16" />
      </label>
      <div className="flex flex-col gap-2">
        <p className="text-sm font-medium">{t("admin.branding.logo")}</p>
        {previewUrl ? (
          // Preview uses the signed URL returned by the API.
          <img src={previewUrl} alt={t("admin.branding.logoAlt")} className="h-16 w-16 rounded object-contain" />
        ) : null}
        <input
          type="file"
          accept=".png,.jpg,.jpeg,.webp,image/png,image/jpeg,image/webp"
          onChange={(event) => void onLogoChange(event.target.files?.[0] ?? null)}
        />
        <p className="text-xs text-zinc-600">{t("admin.branding.logo.help")}</p>
        {logoError ? (
          <p role="alert" className="text-sm text-red-700">
            {t(logoError)}
          </p>
        ) : null}
      </div>
      {status === "saved" ? <p role="status">{t("admin.branding.saved")}</p> : null}
      {status === "failed" ? (
        <p role="alert" className="text-sm text-red-700">
          {t("admin.branding.failed")}
        </p>
      ) : null}
      <button
        type="submit"
        disabled={busy}
        className="self-start rounded-lg bg-[#0B6E4F] px-4 py-2 text-sm font-semibold text-white disabled:opacity-60"
      >
        {t("admin.branding.submit")}
      </button>
    </form>
  );
}

function readInput(form: HTMLFormElement, name: string): string {
  const field = form.elements.namedItem(name);
  return field instanceof HTMLInputElement ? field.value.trim() : "";
}
