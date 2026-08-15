import { renderAppIcon } from "@/lib/pwa/renderAppIcon";

export const size = { width: 32, height: 32 };
export const contentType = "image/png";

export default function Icon() {
  return renderAppIcon({ size: 32 });
}
