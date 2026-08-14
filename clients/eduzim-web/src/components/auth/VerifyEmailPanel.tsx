"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { verifyEmail } from "@/lib/auth/authService";
import { t } from "@/lib/i18n/t";

type VerifyEmailPanelProps = {
  userId?: string;
  token?: string;
};

type Status = "idle" | "pending" | "success" | "error";

export function VerifyEmailPanel({ userId, token }: VerifyEmailPanelProps) {
  const [status, setStatus] = useState<Status>(userId && token ? "pending" : "idle");

  useEffect(() => {
    if (!userId || !token) {
      return;
    }

    let cancelled = false;
    void verifyEmail(userId, token).then(
      () => {
        if (!cancelled) {
          setStatus("success");
        }
      },
      () => {
        if (!cancelled) {
          setStatus("error");
        }
      },
    );

    return () => {
      cancelled = true;
    };
  }, [userId, token]);

  const message =
    status === "pending"
      ? t("auth.verify.pending")
      : status === "success"
        ? t("auth.verify.success")
        : status === "error"
          ? t("auth.verify.error")
          : t("auth.verify.missing");

  return (
    <div className="flex flex-col gap-4">
      <p role="status" className="text-base text-zinc-700">
        {message}
      </p>
      {status === "success" || status === "idle" || status === "error" ? (
        <Link className="text-[#0B6E4F] underline-offset-2 hover:underline" href="/login">
          {t("auth.verify.loginLink")}
        </Link>
      ) : null}
    </div>
  );
}
