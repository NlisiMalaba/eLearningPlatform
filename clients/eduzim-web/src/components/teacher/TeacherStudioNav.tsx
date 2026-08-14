"use client";

import { useEffect, useState } from "react";
import { loadSession } from "@/lib/auth/authService";
import { t } from "@/lib/i18n/t";
import { canManageSchoolContent } from "@/lib/teacher/roles";

export function TeacherStudioNav() {
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    let cancelled = false;
    void loadSession().then((session) => {
      if (!cancelled) {
        setVisible(canManageSchoolContent(session.role));
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
    <nav aria-label={t("teacher.nav.label")} className="flex items-center gap-3 text-sm font-medium">
      <a href="/teacher/content" className="text-[#0B6E4F] underline">
        {t("teacher.nav.content")}
      </a>
      <a href="/teacher/modules" className="text-[#0B6E4F] underline">
        {t("teacher.nav.modules")}
      </a>
    </nav>
  );
}
