import { t } from "@/lib/i18n/t";

export default function OfflinePage() {
  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-16 text-center">
      <h1 className="text-2xl font-semibold tracking-tight text-[#0B6E4F]">
        {t("offline.page.title")}
      </h1>
      <p className="mt-3 max-w-md text-base text-zinc-600">{t("offline.page.body")}</p>
    </main>
  );
}
