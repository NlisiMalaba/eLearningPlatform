import { RegisterForm } from "@/components/auth/RegisterForm";
import { t } from "@/lib/i18n/t";

export default function RegisterPage() {
  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight text-[#0B6E4F]">
        {t("auth.register.title")}
      </h1>
      <p className="mt-3 mb-6 text-base text-zinc-600">{t("auth.register.subtitle")}</p>
      <RegisterForm />
    </>
  );
}
