import { renderAppIcon } from "@/lib/pwa/renderAppIcon";

export function GET() {
  return renderAppIcon({ size: 192 });
}
