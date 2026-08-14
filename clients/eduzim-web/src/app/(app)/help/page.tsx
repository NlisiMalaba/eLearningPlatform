import { t } from "@/lib/i18n/t";

export default function HelpPage() {
  return (
    <article className="mx-auto max-w-xl">
      <h1 className="text-2xl font-semibold tracking-tight">{t("help.title")}</h1>
      <p className="mt-3 text-zinc-600">{t("help.intro")}</p>
      <ul className="mt-6 list-disc space-y-2 pl-5 text-base">
        <li>{t("help.tip.module")}</li>
        <li>{t("help.tip.teacher")}</li>
        <li>{t("help.tip.retry")}</li>
      </ul>
    </article>
  );
}
