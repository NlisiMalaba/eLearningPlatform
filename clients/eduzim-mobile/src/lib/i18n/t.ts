import { en, type MessageKey } from "@/lib/i18n/messages";

export function t(key: MessageKey): string {
  return en[key];
}
