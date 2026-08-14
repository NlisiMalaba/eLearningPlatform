import { BrandingSettingsForm } from "@/components/admin/BrandingSettingsForm";
import { readServerSession } from "@/lib/auth/readServerSession";
import { t } from "@/lib/i18n/t";
import { canManageTenantSettings } from "@/lib/teacher/roles";

export default async function AdminBrandingPage() {
  const session = await readServerSession();
  if (!canManageTenantSettings(session?.role) || !session?.tenantId) {
    return <p role="alert">{t("admin.forbidden")}</p>;
  }

  return <BrandingSettingsForm tenantId={session.tenantId} />;
}
