"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { AuthTextField } from "@/components/auth/AuthFields";
import { login } from "@/lib/auth/authService";
import { loginErrorKey } from "@/lib/auth/mapAuthError";
import { t } from "@/lib/i18n/t";

type LoginFormProps = {
  from: string;
};

export function LoginForm({ from }: LoginFormProps) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [errorKey, setErrorKey] = useState<ReturnType<typeof loginErrorKey> | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();
    setPending(true);
    setErrorKey(null);

    try {
      await login(email, password);
      router.replace(from);
      router.refresh();
    } catch (error: unknown) {
      setErrorKey(loginErrorKey(error));
    } finally {
      setPending(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-5" noValidate>
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
        autoComplete="current-password"
        required
        value={password}
        onChange={(event) => setPassword(event.target.value)}
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
        {t("auth.login.submit")}
      </button>
      <p className="flex flex-col gap-2 text-sm text-zinc-600">
        <Link className="text-[#0B6E4F] underline-offset-2 hover:underline" href="/register">
          {t("auth.login.registerLink")}
        </Link>
        <Link className="text-[#0B6E4F] underline-offset-2 hover:underline" href="/sso">
          {t("auth.login.ssoLink")}
        </Link>
      </p>
    </form>
  );
}
