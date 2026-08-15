import { ClassroomRoom } from "@/components/classroom/ClassroomRoom";
import { readServerSession } from "@/lib/auth/readServerSession";
import { isClassroomSessionId } from "@/lib/classroom/parse";
import { t } from "@/lib/i18n/t";
import { canManageSchoolContent } from "@/lib/teacher/roles";

type ClassroomPageProps = {
  params: Promise<{ sessionId: string }>;
};

export default async function ClassroomSessionPage({ params }: ClassroomPageProps) {
  const { sessionId } = await params;
  if (!isClassroomSessionId(sessionId)) {
    return <p role="alert">{t("classroom.join.invalid")}</p>;
  }

  const session = await readServerSession();
  if (!session?.authenticated || !session.userId) {
    return <p role="alert">{t("classroom.forbidden")}</p>;
  }

  return (
    <ClassroomRoom
      sessionId={sessionId}
      currentUserId={session.userId}
      canControl={canManageSchoolContent(session.role)}
    />
  );
}
