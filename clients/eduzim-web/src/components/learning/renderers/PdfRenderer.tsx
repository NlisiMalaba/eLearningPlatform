"use client";

import { t } from "@/lib/i18n/t";

type PdfRendererProps = {
  src: string;
  title: string;
};

export function PdfRenderer({ src, title }: PdfRendererProps) {
  return (
    <div className="flex flex-col gap-3">
      <iframe
        title={`${t("content.pdf.label")}: ${title}`}
        src={src}
        className="h-[70vh] w-full rounded-xl border border-zinc-200 bg-white"
      />
      <a
        href={src}
        target="_blank"
        rel="noreferrer"
        className="text-sm font-medium text-[#0B6E4F] underline"
      >
        {t("content.pdf.open")}
      </a>
    </div>
  );
}
