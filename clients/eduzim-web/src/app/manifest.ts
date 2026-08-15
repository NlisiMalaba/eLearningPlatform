import type { MetadataRoute } from "next";
import { t } from "@/lib/i18n/t";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: t("app.name"),
    short_name: t("app.name"),
    description: t("app.home.subtitle"),
    start_url: "/",
    display: "standalone",
    background_color: "#F7F5F0",
    theme_color: "#0B6E4F",
    orientation: "portrait",
    icons: [
      {
        src: "/icons/icon-192",
        sizes: "192x192",
        type: "image/png",
        purpose: "any",
      },
      {
        src: "/icons/icon-512",
        sizes: "512x512",
        type: "image/png",
        purpose: "maskable",
      },
    ],
  };
}
