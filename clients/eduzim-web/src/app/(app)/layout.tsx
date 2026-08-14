import type { ReactNode } from "react";
import { AdminStudioNav } from "@/components/admin/AdminStudioNav";
import { AccessibilitySettingsDialog } from "@/components/accessibility/AccessibilitySettingsDialog";
import { SignOutButton } from "@/components/auth/SignOutButton";
import { TeacherStudioNav } from "@/components/teacher/TeacherStudioNav";
import { ZimBotWidget } from "@/components/zimbot/ZimBotWidget";
import { t } from "@/lib/i18n/t";

type AppLayoutProps = {
  children: ReactNode;
};

export default function AppLayout({ children }: AppLayoutProps) {
  return (
    <div className="flex flex-1 flex-col">
      <header className="flex items-center justify-between border-b border-black/5 bg-white px-4 py-3">
        <p className="text-lg font-semibold tracking-tight text-[#0B6E4F]">{t("app.name")}</p>
        <div className="flex items-center gap-3">
          <AdminStudioNav />
          <TeacherStudioNav />
          <AccessibilitySettingsDialog />
          <SignOutButton />
        </div>
      </header>
      <main id="main-content" className="flex-1 px-4 py-6">
        {children}
      </main>
      <ZimBotWidget />
    </div>
  );
}
