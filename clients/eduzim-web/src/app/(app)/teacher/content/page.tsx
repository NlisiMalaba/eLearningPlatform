import { ContentStudio } from "@/components/teacher/ContentStudio";
import { readServerSession } from "@/lib/auth/readServerSession";
import { t } from "@/lib/i18n/t";
import { canManageSchoolContent } from "@/lib/teacher/roles";

export default async function TeacherContentPage() {
  const session = await readServerSession();
  if (!canManageSchoolContent(session?.role)) {
    return <p role="alert">{t("teacher.forbidden")}</p>;
  }

  return <ContentStudio />;
}
