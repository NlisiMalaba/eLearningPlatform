import { VerifyEmailPanel } from "@/components/auth/VerifyEmailPanel";
import { t } from "@/lib/i18n/t";

type VerifyEmailPageProps = {
  searchParams: Promise<{ userId?: string; token?: string }>;
};

export default async function VerifyEmailPage({ searchParams }: VerifyEmailPageProps) {
  const params = await searchParams;

  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight text-[#0B6E4F]">
        {t("auth.verify.title")}
      </h1>
      <p className="mt-3 mb-6 text-base text-zinc-600">{t("auth.verify.subtitle")}</p>
      <VerifyEmailPanel userId={params.userId} token={params.token} />
    </>
  );
}
