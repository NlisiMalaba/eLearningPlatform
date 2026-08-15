"use client";

import { t } from "@/lib/i18n/t";

type AudioRendererProps = {
  src: string;
  title: string;
  transcriptUrl: string | null;
  transcriptText: string | null;
};

export function AudioRenderer({
  src,
  title,
  transcriptUrl,
  transcriptText,
}: AudioRendererProps) {
  return (
    <div className="flex flex-col gap-4">
      <audio className="w-full" controls aria-label={`${t("content.audio.label")}: ${title}`}>
        <source src={src} />
      </audio>
      {transcriptUrl ? (
        <section aria-labelledby="transcript-heading" className="rounded-xl border border-zinc-200 bg-white p-4">
          <div className="flex items-center justify-between gap-3">
            <h3 id="transcript-heading" className="text-sm font-semibold">
              {t("content.audio.transcript")}
            </h3>
            <a
              href={transcriptUrl}
              target="_blank"
              rel="noreferrer"
              className="text-sm font-medium text-[#0B6E4F] underline"
            >
              {t("content.audio.transcriptOpen")}
            </a>
          </div>
          {transcriptText ? (
            <pre className="mt-3 max-h-64 overflow-auto whitespace-pre-wrap font-sans text-sm">
              {transcriptText}
            </pre>
          ) : null}
        </section>
      ) : (
        <p className="text-sm text-zinc-600">{t("content.audio.transcriptMissing")}</p>
      )}
    </div>
  );
}
