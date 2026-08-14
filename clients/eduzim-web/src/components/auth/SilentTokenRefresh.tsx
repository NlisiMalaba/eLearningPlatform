"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { loadSession, refreshSession } from "@/lib/auth/authService";
import { SILENT_REFRESH_LEAD_SECONDS } from "@/lib/auth/cookies";
import { SESSION_EXPIRED_EVENT } from "@/lib/auth/sessionEvents";
import { safeRedirectPath } from "@/lib/auth/safeRedirect";

export function SilentTokenRefresh() {
  const router = useRouter();

  useEffect(() => {
    const onExpired = (event: Event) => {
      const detail = (event as CustomEvent<{ from?: string }>).detail;
      const from = encodeURIComponent(safeRedirectPath(detail?.from));
      router.replace(`/login?from=${from}`);
    };

    window.addEventListener(SESSION_EXPIRED_EVENT, onExpired);
    return () => {
      window.removeEventListener(SESSION_EXPIRED_EVENT, onExpired);
    };
  }, [router]);

  useEffect(() => {
    let cancelled = false;
    let timer: ReturnType<typeof setTimeout> | undefined;

    const schedule = (expiresAtIso: string | undefined) => {
      if (timer) {
        clearTimeout(timer);
      }

      if (!expiresAtIso) {
        return;
      }

      const delayMs =
        new Date(expiresAtIso).getTime() - Date.now() - SILENT_REFRESH_LEAD_SECONDS * 1000;
      timer = setTimeout(() => {
        void runRefresh();
      }, Math.max(0, delayMs));
    };

    const runRefresh = async () => {
      try {
        const session = await refreshSession();
        if (!cancelled) {
          schedule(session.accessTokenExpiresAt);
        }
      } catch {
        // Expired refresh is handled by middleware and apiFetch redirects.
      }
    };

    const boot = async () => {
      const session = await loadSession();
      if (cancelled || !session.authenticated) {
        return;
      }

      schedule(session.accessTokenExpiresAt);
    };

    void boot();
    const onVisible = () => {
      if (document.visibilityState === "visible") {
        void boot();
      }
    };
    document.addEventListener("visibilitychange", onVisible);

    return () => {
      cancelled = true;
      if (timer) {
        clearTimeout(timer);
      }
      document.removeEventListener("visibilitychange", onVisible);
    };
  }, []);

  return null;
}
