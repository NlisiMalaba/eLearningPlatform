import { ScheduleClassroomForm } from "@/components/classroom/ScheduleClassroomForm";
import { readServerSession } from "@/lib/auth/readServerSession";
import { t } from "@/lib/i18n/t";
import { canManageSchoolContent } from "@/lib/teacher/roles";

export default async function TeacherClassroomsPage() {
  const session = await readServerSession();
  if (!canManageSchoolContent(session?.role)) {
    return <p role="alert">{t("teacher.forbidden")}</p>;
  }

  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight">{t("classroom.schedule.title")}</h1>
      <p className="mt-3 max-w-xl text-base text-zinc-600">{t("classroom.schedule.subtitle")}</p>
      <ScheduleClassroomForm />
    </>
  );
}
