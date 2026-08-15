"use client";

import { useCallback, useId, useRef, useState } from "react";
import { AccessibilitySettingsForm } from "@/components/accessibility/AccessibilitySettingsForm";
import { useFocusTrap } from "@/hooks/useFocusTrap";
import { t } from "@/lib/i18n/t";

export function AccessibilitySettingsDialog() {
  const [open, setOpen] = useState(false);
  const dialogRef = useRef<HTMLDivElement>(null);
  const titleId = useId();
  const close = useCallback(() => setOpen(false), []);
  useFocusTrap(open, dialogRef, close);

  return (
    <>
      <button
        type="button"
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-controls={open ? "accessibility-settings-dialog" : undefined}
        onClick={() => setOpen(true)}
        className="rounded-md px-3 py-1.5 text-sm font-medium text-[#0B6E4F] outline-none hover:bg-zinc-100 focus-visible:ring-2 focus-visible:ring-[#0B6E4F]"
      >
        {t("a11y.menu")}
      </button>
      {open ? (
        <div className="fixed inset-0 z-50 flex items-start justify-center px-4 py-16">
          <div className="absolute inset-0 bg-black/40" onClick={close} />
          <div
            id="accessibility-settings-dialog"
            ref={dialogRef}
            role="dialog"
            aria-modal="true"
            aria-labelledby={titleId}
            className="relative z-10 w-full max-w-md rounded-2xl bg-white p-6 shadow-lg ring-1 ring-black/10"
          >
            <AccessibilitySettingsForm headingId={titleId} />
            <button
              type="button"
              onClick={close}
              className="mt-6 rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium outline-none hover:bg-zinc-50 focus-visible:ring-2 focus-visible:ring-[#0B6E4F]"
            >
              {t("a11y.close")}
            </button>
          </div>
        </div>
      ) : null}
    </>
  );
}
