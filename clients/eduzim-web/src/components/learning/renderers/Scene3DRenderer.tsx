"use client";

import { useEffect, useRef } from "react";
import { t } from "@/lib/i18n/t";
import {
  mountInteractiveScene,
  type Scene3DHandle,
} from "@/lib/content/mountInteractiveScene";

type Scene3DRendererProps = {
  src: string;
  title: string;
};

export function Scene3DRenderer({ src, title }: Scene3DRendererProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const handleRef = useRef<Scene3DHandle | null>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) {
      return;
    }

    let cancelled = false;
    void mountInteractiveScene(canvas, src).then((handle) => {
      if (cancelled) {
        handle.dispose();
        return;
      }

      handleRef.current = handle;
    });

    return () => {
      cancelled = true;
      handleRef.current?.dispose();
      handleRef.current = null;
    };
  }, [src]);

  return (
    <div className="flex flex-col gap-3">
      <canvas
        ref={canvasRef}
        tabIndex={0}
        role="img"
        aria-label={`${t("content.scene.label")}: ${title}. ${t("content.scene.hint")}`}
        className="h-[60vh] w-full rounded-xl border border-zinc-200 bg-[#F7F5F0]"
      />
      <p className="text-sm text-zinc-600">{t("content.scene.hint")}</p>
      <button
        type="button"
        className="self-start rounded-lg border border-zinc-300 px-3 py-2 text-sm font-medium"
        onClick={() => handleRef.current?.reset()}
      >
        {t("content.scene.reset")}
      </button>
    </div>
  );
}
