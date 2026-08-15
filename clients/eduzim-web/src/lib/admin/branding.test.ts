import { describe, expect, it } from "vitest";
import {
  formatStorageBytes,
  isHexColour,
  MAX_LOGO_BYTES,
  toColorInputValue,
  validateLogoFile,
} from "@/lib/admin/branding";

function file(name: string, size: number, type: string): File {
  return new File([new Uint8Array(Math.min(size, 8))], name, { type });
}

describe("branding helpers", () => {
  it("accepts hex colours and expands short values for the colour picker", () => {
    expect(isHexColour("#0B6E4F")).toBe(true);
    expect(isHexColour("green")).toBe(false);
    expect(toColorInputValue("#0B6")).toBe("#00BB66");
  });

  it("rejects oversized or non-image logos before upload", () => {
    expect(validateLogoFile(file("logo.png", 10, "image/png"))).toBeNull();
    const big = file("logo.png", 8, "image/png");
    Object.defineProperty(big, "size", { value: MAX_LOGO_BYTES + 1 });
    expect(validateLogoFile(big)).toBe("size");
    expect(validateLogoFile(file("notes.pdf", 10, "application/pdf"))).toBe("type");
  });

  it("formats storage usage for the admin dashboard", () => {
    expect(formatStorageBytes(512)).toBe("512 B");
    expect(formatStorageBytes(2048)).toBe("2.0 KB");
    expect(formatStorageBytes(2 * 1024 * 1024)).toBe("2.0 MB");
  });
});
