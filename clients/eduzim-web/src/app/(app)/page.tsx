import { StudentDashboard } from "@/components/dashboard/StudentDashboard";
import { serverApiFetch } from "@/lib/api/serverFetch";
import { readServerSession } from "@/lib/auth/readServerSession";
import { loadStudentDashboard } from "@/lib/dashboard/dashboardService";
import type { StudentDashboardData } from "@/lib/dashboard/types";
import { t } from "@/lib/i18n/t";

export default async function HomePage() {
  const session = await readServerSession();
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
