import type { ReactNode } from "react";
import { t } from "@/lib/i18n/t";

type AppLayoutProps = {
  children: ReactNode;
};

export default function AppLayout({ children }: AppLayoutProps) {
  return (
    <div className="flex flex-1 flex-col">
      <header className="border-b border-black/5 bg-white px-4 py-3">
        <p className="text-lg font-semibold tracking-tight text-[#0B6E4F]">
          {t("app.name")}
        </p>
      </header>
      <div className="flex-1 px-4 py-6">{children}</div>
    </div>
  );
}
