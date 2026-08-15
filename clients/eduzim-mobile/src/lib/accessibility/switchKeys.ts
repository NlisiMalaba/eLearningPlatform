export type SwitchAction = "next" | "prev" | "activate";

export function interpretSwitchKey(key: string, shiftKey = false): SwitchAction | null {
  if (key === " " || key === "Spacebar" || key === "Space") {
    return "activate";
  }

  const normalized = key.trim().toLowerCase();
  if (normalized === "tab") {
    return shiftKey ? "prev" : "next";
  }

  if (normalized === "arrowright" || normalized === "arrowdown" || normalized === "pagedown") {
    return "next";
  }

  if (normalized === "arrowleft" || normalized === "arrowup" || normalized === "pageup") {
    return "prev";
  }

  if (
    normalized === "enter" ||
    normalized === " " ||
    normalized === "spacebar" ||
    normalized === "space" ||
    normalized === "select" ||
    normalized === "numpadenter"
  ) {
    return "activate";
  }

  return null;
}

export function nextIndex(current: number, length: number): number {
  if (length <= 0) {
    return -1;
  }

  if (current < 0 || current >= length) {
    return 0;
  }

  return (current + 1) % length;
}

export function previousIndex(current: number, length: number): number {
  if (length <= 0) {
    return -1;
  }

  if (current < 0 || current >= length) {
    return length - 1;
  }

  return (current - 1 + length) % length;
}
