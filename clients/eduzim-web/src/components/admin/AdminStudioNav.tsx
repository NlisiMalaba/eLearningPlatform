"use client";

import { useEffect, useState } from "react";
import { loadSession } from "@/lib/auth/authService";
import { t } from "@/lib/i18n/t";
import { canManageTenantSettings } from "@/lib/teacher/roles";

export function AdminStudioNav() {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void loadSession().then((session) => {
      if (!cancelled) {
        setVisible(canManageTenantSettings(session.role));
      }
    });
    return () => {
      cancelled = true;
    };
  }, []);

  if (!visible) {
    return null;
  }

  return (
    <nav aria-label={t("admin.nav.label")} className="flex items-center gap-3 text-sm font-medium">
      <a href="/admin" className="text-[#0B6E4F] underline">
        {t("admin.nav.dashboard")}
      </a>
      <a href="/admin/branding" className="text-[#0B6E4F] underline">
        {t("admin.nav.branding")}
      </a>
    </nav>
  );
}
