export const PRESCHOOL_LANGUAGES = ["en", "sn", "nd"] as const;

export type PreschoolLanguage = (typeof PRESCHOOL_LANGUAGES)[number];

export const DEFAULT_PRESCHOOL_LANGUAGE: PreschoolLanguage = "en";

export function isPreschoolLanguage(value: unknown): value is PreschoolLanguage {
  return typeof value === "string" && (PRESCHOOL_LANGUAGES as readonly string[]).includes(value);
}
