export const FONT_SIZES = ["Small", "Medium", "Large", "ExtraLarge"] as const;

export type FontSize = (typeof FONT_SIZES)[number];

export const DEFAULT_FONT_SIZE: FontSize = "Medium";

export function isValidFontSize(value: unknown): value is FontSize {
  return typeof value === "string" && (FONT_SIZES as readonly string[]).includes(value);
}

export function parseFontSize(value: unknown): FontSize {
  if (!isValidFontSize(value)) {
    throw new FontSizeValidationError(value);
  }

  return value;
}

export function tryParseFontSize(value: unknown): FontSize | null {
  return isValidFontSize(value) ? value : null;
}

export class FontSizeValidationError extends Error {
  public readonly invalidValue: unknown;

  constructor(invalidValue: unknown) {
    super("Font size must be Small, Medium, Large, or ExtraLarge.");
    this.name = "FontSizeValidationError";
    this.invalidValue = invalidValue;
  }
}
