export const ZIMBOT_LANGUAGES = ["English", "Shona", "Ndebele", "Kalanga"] as const;

export type ZimBotLanguage = (typeof ZIMBOT_LANGUAGES)[number];

export const DEFAULT_ZIMBOT_LANGUAGE: ZimBotLanguage = "English";

export function isZimBotLanguage(value: unknown): value is ZimBotLanguage {
  return typeof value === "string" && (ZIMBOT_LANGUAGES as readonly string[]).includes(value);
}

export function parseZimBotLanguage(value: unknown): ZimBotLanguage {
  if (typeof value !== "string") {
    return DEFAULT_ZIMBOT_LANGUAGE;
  }

  const key = value.trim().toLowerCase();
  switch (key) {
    case "en":
    case "eng":
    case "english":
      return "English";
    case "sn":
    case "sna":
    case "shona":
      return "Shona";
    case "nd":
    case "nde":
    case "nr":
    case "ndebele":
      return "Ndebele";
    case "kck":
    case "kalanga":
      return "Kalanga";
    default:
      return isZimBotLanguage(value) ? value : DEFAULT_ZIMBOT_LANGUAGE;
  }
}
