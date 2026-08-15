"use client";

import { t } from "@/lib/i18n/t";
import { useConnectivityStore } from "@/lib/offline/connectivityStore";

export function OfflineBanner() {
  const isOnline = useConnectivityStore((state) => state.isOnline);

  if (isOnline) {
    return null;
  }

  return (
    <div
      role="status"
      aria-live="polite"
      className="bg-[#CE1126] px-4 py-3 text-center text-sm font-medium text-white"
    >
      {t("offline.banner")}
    </div>
  );
}
