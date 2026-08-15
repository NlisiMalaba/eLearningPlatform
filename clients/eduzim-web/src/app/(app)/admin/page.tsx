import { AdminDashboard } from "@/components/admin/AdminDashboard";
import { readServerSession } from "@/lib/auth/readServerSession";
import { t } from "@/lib/i18n/t";
import { canManageTenantSettings } from "@/lib/teacher/roles";

export default async function AdminDashboardPage() {
  const session = await readServerSession();
  if (!canManageTenantSettings(session?.role) || !session?.tenantId) {
    return <p role="alert">{t("admin.forbidden")}</p>;
  }

  return <AdminDashboard tenantId={session.tenantId} />;
}
