export const MAX_LOGO_BYTES = 2 * 1024 * 1024;

export const HEX_COLOUR_PATTERN = /^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$/;

export type LogoValidationError = "type" | "size" | "empty";

export function isHexColour(value: string): boolean {
  return HEX_COLOUR_PATTERN.test(value.trim());
}

export function toColorInputValue(hex: string): string {
  const value = hex.trim();
  if (/^#[0-9A-Fa-f]{6}$/.test(value)) {
    return value;
  }

  if (/^#[0-9A-Fa-f]{3}$/.test(value)) {
    const r = value[1];
    const g = value[2];
    const b = value[3];
    return `#${r}${r}${g}${g}${b}${b}`;
  }

  return "#1976D2";
}

export function isAllowedLogoFile(file: File): boolean {
  const mime = file.type.toLowerCase();
  const name = file.name.toLowerCase();
  return (
    mime === "image/png" ||
    mime === "image/jpeg" ||
    mime === "image/webp" ||
    name.endsWith(".png") ||
    name.endsWith(".jpg") ||
    name.endsWith(".jpeg") ||
    name.endsWith(".webp")
  );
}

export function validateLogoFile(file: File): LogoValidationError | null {
  if (file.size <= 0) {
    return "empty";
  }

  if (!isAllowedLogoFile(file)) {
    return "type";
  }

  if (file.size > MAX_LOGO_BYTES) {
    return "size";
  }

  return null;
}

export function formatStorageBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  const megabytes = bytes / (1024 * 1024);
  if (megabytes < 1) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${megabytes.toFixed(1)} MB`;
}
