import { AssessmentResultsStudio } from "@/components/teacher/AssessmentResultsStudio";
import { readServerSession } from "@/lib/auth/readServerSession";
import { t } from "@/lib/i18n/t";
import { canManageSchoolContent } from "@/lib/teacher/roles";

type TeacherAssessmentPageProps = {
  params: Promise<{ assessmentId: string }>;
};

export default async function TeacherAssessmentPage({ params }: TeacherAssessmentPageProps) {
  const session = await readServerSession();
  if (!canManageSchoolContent(session?.role)) {
    return <p role="alert">{t("teacher.forbidden")}</p>;
  }

  const { assessmentId } = await params;
  return <AssessmentResultsStudio assessmentId={assessmentId} />;
}
