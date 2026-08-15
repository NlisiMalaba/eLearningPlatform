import { ClassroomJoinForm } from "@/components/classroom/ClassroomJoinForm";
import { t } from "@/lib/i18n/t";

export default function ClassroomsIndexPage() {
  return (
    <>
      <h1 className="text-2xl font-semibold tracking-tight">{t("classroom.join.title")}</h1>
      <p className="mt-3 max-w-xl text-base text-zinc-600">{t("classroom.join.subtitle")}</p>
      <ClassroomJoinForm />
    </>
  );
}
