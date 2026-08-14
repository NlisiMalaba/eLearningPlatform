import { t } from "@/lib/i18n/t";

export default function VerifyEmailPage() {
  return (
    <main>
      <h1 className="text-2xl font-semibold tracking-tight text-[#0B6E4F]">
        {t("auth.verify.title")}
      </h1>
      <p className="mt-3 text-base text-zinc-600">{t("auth.verify.subtitle")}</p>
    </main>
  );
}
