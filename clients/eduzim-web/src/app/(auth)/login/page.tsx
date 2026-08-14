import { LoginForm } from "@/components/auth/LoginForm";
import { safeRedirectPath } from "@/lib/auth/safeRedirect";
import { t } from "@/lib/i18n/t";

type LoginPageProps = {
  searchParams: Promise<{ from?: string }>;
};

export default async function LoginPage({ searchParams }: LoginPageProps) {
  const params = await searchParams;
  const from = safeRedirectPath(params.from);

  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight text-[#0B6E4F]">
        {t("auth.login.title")}
      </h1>
      <p className="mt-3 mb-6 text-base text-zinc-600">{t("auth.login.subtitle")}</p>
      <LoginForm from={from} />
    </>
  );
}
