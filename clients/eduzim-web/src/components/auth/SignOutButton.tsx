"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { logout } from "@/lib/auth/authService";
import { t } from "@/lib/i18n/t";

export function SignOutButton() {
  const router = useRouter();
  const [pending, setPending] = useState(false);

  async function onClick(): Promise<void> {
    setPending(true);
    try {
      await logout();
    } finally {
      router.replace("/login");
      router.refresh();
    }
  }

  return (
    <button
      type="button"
      onClick={() => void onClick()}
      disabled={pending}
      aria-label={t("app.signOut")}
      className="rounded-md px-3 py-1.5 text-sm font-medium text-[#0B6E4F] outline-none hover:bg-zinc-100 focus-visible:ring-2 focus-visible:ring-[#0B6E4F] disabled:opacity-60"
    >
      {t("app.signOut")}
    </button>
  );
}
