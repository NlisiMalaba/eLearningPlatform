import { t } from "@/lib/i18n/t";

export default function HomePage() {
  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight">{t("app.home.title")}</h1>
      <p className="mt-3 max-w-xl text-base text-zinc-600">{t("app.home.subtitle")}</p>
    </>
  );
}
