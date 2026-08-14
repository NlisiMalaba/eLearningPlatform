"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import { AuthTextField } from "@/components/auth/AuthFields";
import { startSso } from "@/lib/auth/authService";
import { loginErrorKey } from "@/lib/auth/mapAuthError";
import { t } from "@/lib/i18n/t";

export function SsoForm() {
  const [tenantId, setTenantId] = useState("");
  const [errorKey, setErrorKey] = useState<ReturnType<typeof loginErrorKey> | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setPending(true);
    setErrorKey(null);

    try {
      const { redirectUrl } = await startSso(tenantId.trim());
      window.location.assign(redirectUrl);
    } catch (error: unknown) {
      setErrorKey(loginErrorKey(error));
      setPending(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-5" noValidate>
      <AuthTextField
        id="tenantId"
        name="tenantId"
        type="text"
        label={t("auth.field.tenantId")}
        autoComplete="off"
        required
        value={tenantId}
        onChange={(event) => setTenantId(event.target.value)}
      />
      {errorKey ? (
        <p role="alert" className="text-sm text-[#CE1126]">
          {t(errorKey)}
        </p>
      ) : null}
      <button
        type="submit"
        disabled={pending}
        className="rounded-lg bg-[#0B6E4F] px-4 py-2.5 text-base font-medium text-white outline-none hover:bg-[#095c42] focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-[#0B6E4F] disabled:opacity-60"
      >
        {t("auth.sso.submit")}
      </button>
      <Link className="text-sm text-[#0B6E4F] underline-offset-2 hover:underline" href="/login">
        {t("auth.sso.loginLink")}
      </Link>
    </form>
  );
}
