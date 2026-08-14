import { AssessmentStudio } from "@/components/teacher/AssessmentStudio";
import { readServerSession } from "@/lib/auth/readServerSession";
import { t } from "@/lib/i18n/t";
import { canManageSchoolContent } from "@/lib/teacher/roles";

export default async function TeacherAssessmentsPage() {
  const session = await readServerSession();
  if (!canManageSchoolContent(session?.role)) {
    return <p role="alert">{t("teacher.forbidden")}</p>;
  }

  return <AssessmentStudio />;
}
