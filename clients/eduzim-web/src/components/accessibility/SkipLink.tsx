import { t } from "@/lib/i18n/t";

export function SkipLink() {
  return (
    <a href="#main-content" className="skip-link">
      {t("a11y.skipToContent")}
    </a>
  );
}
