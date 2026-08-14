import { describe, expect, it } from "vitest";
import {
  FONT_SIZES,
  FontSizeValidationError,
  isValidFontSize,
  parseFontSize,
  tryParseFontSize,
} from "@/lib/accessibility/fontSize";

describe("font size preference validation", () => {
  it("accepts Small, Medium, Large, and ExtraLarge", () => {
    for (const size of FONT_SIZES) {
      expect(isValidFontSize(size)).toBe(true);
      expect(parseFontSize(size)).toBe(size);
    }
  });

  it("rejects any other value with a validation error", () => {
    const invalid = ["tiny", "medium", "XL", "", 12, null, undefined, "Extra-Large"];

    for (const value of invalid) {
      expect(isValidFontSize(value)).toBe(false);
      expect(tryParseFontSize(value)).toBeNull();
      expect(() => parseFontSize(value)).toThrow(FontSizeValidationError);
    }
  });
});
