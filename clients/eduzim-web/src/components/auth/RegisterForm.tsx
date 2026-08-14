"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import { AuthSelectField, AuthTextField } from "@/components/auth/AuthFields";
import { registerAccount } from "@/lib/auth/authService";
import { registerErrorKey } from "@/lib/auth/mapAuthError";
import type { PublicRegisterRole } from "@/lib/auth/types";
import { t } from "@/lib/i18n/t";

export function RegisterForm() {
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<PublicRegisterRole>("ParentGuardian");
  const [errorKey, setErrorKey] = useState<ReturnType<typeof registerErrorKey> | null>(null);
  const [success, setSuccess] = useState(false);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setPending(true);
    setErrorKey(null);

    try {
      await registerAccount({ fullName, email, password, role });
      setSuccess(true);
    } catch (error: unknown) {
      setErrorKey(registerErrorKey(error));
    } finally {
      setPending(false);
    }
  }

  if (success) {
    return (
      <div className="flex flex-col gap-4">
        <p role="status" className="text-base text-zinc-700">
          {t("auth.register.success")}
        </p>
        <Link className="text-[#0B6E4F] underline-offset-2 hover:underline" href="/login">
          {t("auth.verify.loginLink")}
        </Link>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-5" noValidate>
      <AuthTextField
        id="fullName"
        name="fullName"
        type="text"
        label={t("auth.field.fullName")}
        autoComplete="name"
        required
        value={fullName}
        onChange={(event) => setFullName(event.target.value)}
      />
      <AuthTextField
        id="email"
        name="email"
        type="email"
        label={t("auth.field.email")}
        autoComplete="email"
        required
        value={email}
        onChange={(event) => setEmail(event.target.value)}
      />
      <AuthTextField
        id="password"
        name="password"
        type="password"
        label={t("auth.field.password")}
        autoComplete="new-password"
        required
        minLength={8}
        hint={t("auth.password.hint")}
        value={password}
        onChange={(event) => setPassword(event.target.value)}
      />
      <AuthSelectField
        id="role"
        name="role"
        label={t("auth.field.role")}
        value={role}
        onChange={(event) => setRole(event.target.value as PublicRegisterRole)}
      >
        <option value="ParentGuardian">{t("auth.register.role.parent")}</option>
        <option value="SchoolAdmin">{t("auth.register.role.schoolAdmin")}</option>
      </AuthSelectField>
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
        {t("auth.register.submit")}
      </button>
      <Link className="text-sm text-[#0B6E4F] underline-offset-2 hover:underline" href="/login">
        {t("auth.register.loginLink")}
      </Link>
    </form>
  );
}
