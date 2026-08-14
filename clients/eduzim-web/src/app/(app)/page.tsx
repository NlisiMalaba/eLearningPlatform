import { StudentDashboard } from "@/components/dashboard/StudentDashboard";
import { serverApiFetch } from "@/lib/api/serverFetch";
import { readServerSession } from "@/lib/auth/readServerSession";
import { loadStudentDashboard } from "@/lib/dashboard/dashboardService";
import type { StudentDashboardData } from "@/lib/dashboard/types";
import { t } from "@/lib/i18n/t";
import { canManageSchoolContent, canManageTenantSettings } from "@/lib/teacher/roles";

export default async function HomePage() {
  const session = await readServerSession();
  if (canManageTenantSettings(session?.role)) {
    return <AdminHome />;
  }

  if (canManageSchoolContent(session?.role)) {
    return <TeacherHome />;
  }

  if (session?.role !== "Student" || !session.userId) {
    return <WelcomeHome />;
  }

  const initialData = await loadInitialDashboard(session.userId, session.tenantId);
  return (
    <StudentDashboard
      studentId={session.userId}
      tenantId={session.tenantId}
      initialData={initialData}
    />
  );
}

function AdminHome() {
  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight">{t("admin.home.title")}</h1>
      <p className="mt-3 max-w-xl text-base text-zinc-600">{t("admin.home.subtitle")}</p>
      <div className="mt-6 flex flex-col gap-2">
        <a href="/admin" className="font-medium text-[#0B6E4F] underline">
          {t("admin.nav.dashboard")}
        </a>
        <a href="/admin/branding" className="font-medium text-[#0B6E4F] underline">
          {t("admin.nav.branding")}
        </a>
        <a href="/teacher/content" className="font-medium text-[#0B6E4F] underline">
          {t("teacher.nav.content")}
        </a>
        <a href="/teacher/modules" className="font-medium text-[#0B6E4F] underline">
          {t("teacher.nav.modules")}
        </a>
        <a href="/teacher/assessments" className="font-medium text-[#0B6E4F] underline">
          {t("teacher.nav.assessments")}
        </a>
      </div>
    </>
  );
}

function TeacherHome() {
  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight">{t("teacher.home.title")}</h1>
      <p className="mt-3 max-w-xl text-base text-zinc-600">{t("teacher.home.subtitle")}</p>
      <div className="mt-6 flex flex-col gap-2">
        <a href="/teacher/content" className="font-medium text-[#0B6E4F] underline">
          {t("teacher.nav.content")}
        </a>
        <a href="/teacher/modules" className="font-medium text-[#0B6E4F] underline">
          {t("teacher.nav.modules")}
        </a>
        <a href="/teacher/assessments" className="font-medium text-[#0B6E4F] underline">
          {t("teacher.nav.assessments")}
        </a>
      </div>
    </>
  );
}

function WelcomeHome() {
  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight">{t("app.home.title")}</h1>
      <p className="mt-3 max-w-xl text-base text-zinc-600">{t("app.home.subtitle")}</p>
    </>
  );
}

async function loadInitialDashboard(
  studentId: string,
  tenantId: string | undefined,
): Promise<StudentDashboardData | null> {
  try {
    return await loadStudentDashboard(studentId, tenantId, serverApiFetch);
  } catch {
    return null;
  }
}
