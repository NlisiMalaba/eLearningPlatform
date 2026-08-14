import { ModuleBuilder } from "@/components/teacher/ModuleBuilder";
import { readServerSession } from "@/lib/auth/readServerSession";
import { t } from "@/lib/i18n/t";
import { canManageSchoolContent } from "@/lib/teacher/roles";

type TeacherModulePageProps = {
  params: Promise<{ moduleId: string }>;
};

export default async function TeacherModuleBuilderPage({ params }: TeacherModulePageProps) {
  const session = await readServerSession();
  if (!canManageSchoolContent(session?.role)) {
    return <p role="alert">{t("teacher.forbidden")}</p>;
  }

  const { moduleId } = await params;
  return <ModuleBuilder moduleId={moduleId} />;
}
