"use client";

import { t } from "@/lib/i18n/t";

export function UnsupportedRenderer() {
  return <p className="text-sm text-zinc-600">{t("content.unsupported")}</p>;
}
