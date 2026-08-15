import { AccessibilitySettingsForm } from "@/components/accessibility/AccessibilitySettingsForm";
import { t } from "@/lib/i18n/t";

export default function SettingsPage() {
  return (
    <>
      <h1 className="mb-6 text-2xl font-semibold tracking-tight">{t("a11y.panel.title")}</h1>
      <div className="max-w-md rounded-2xl bg-white p-6 shadow-sm ring-1 ring-black/5">
        <AccessibilitySettingsForm />
      </div>
    </>
  );
}
