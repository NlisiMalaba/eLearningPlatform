import { dictionaries, type Locale, type MessageKey } from "@/lib/i18n/messages";

export const DEFAULT_LOCALE: Locale = "en";

export function t(key: MessageKey, locale: Locale = DEFAULT_LOCALE): string {
  return dictionaries[locale][key] ?? dictionaries.en[key];
}
