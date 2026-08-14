"use client";

import { usePathname } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { ZimBotPanel } from "@/components/zimbot/ZimBotPanel";
import { loadSession } from "@/lib/auth/authService";
import { t } from "@/lib/i18n/t";
import { isLearningPath } from "@/lib/zimbot/learningContext";
import { useZimBotStore } from "@/lib/zimbot/store";

export function ZimBotWidget() {
  const pathname = usePathname();
  const open = useZimBotStore((state) => state.open);
  const setOpen = useZimBotStore((state) => state.setOpen);
  const [studentId, setStudentId] = useState<string | null>(null);
  const close = useCallback(() => setOpen(false), [setOpen]);

  useEffect(() => {
    let cancelled = false;
    void loadSession().then((session) => {
      if (cancelled) {
        return;
      }

      if (session.authenticated && session.role === "Student" && session.userId) {
        setStudentId(session.userId);
        return;
      }

      setStudentId(null);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  if (!studentId || !isLearningPath(pathname)) {
    return null;
  }

  return (
    <div className="fixed bottom-4 right-4 z-40 flex flex-col items-end gap-3">
      {open ? <ZimBotPanel studentId={studentId} onClose={close} /> : null}
      <button
        type="button"
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-controls={open ? "zimbot-panel" : undefined}
        onClick={() => setOpen(!open)}
        className="rounded-full bg-[#0B6E4F] px-4 py-3 text-sm font-semibold text-white shadow-lg"
      >
        {t("zimbot.launcher")}
      </button>
    </div>
  );
}
