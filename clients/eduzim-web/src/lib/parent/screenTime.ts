export const MIN_SCREEN_TIME_SECONDS = 1;
export const MAX_SCREEN_TIME_SECONDS = 86_400;
export const DEFAULT_LIMIT_HOURS = 2;

export function secondsToHours(seconds: number | null): number | null {
  if (seconds === null || !Number.isFinite(seconds) || seconds <= 0) {
    return null;
  }

  return Math.round((seconds / 3_600) * 100) / 100;
}

export function hoursToSeconds(hours: number): number {
  return Math.round(hours * 3_600);
}

export function parseHoursInput(value: string): number | null {
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    return null;
  }

  const hours = Number(trimmed);
  if (!Number.isFinite(hours)) {
    return null;
  }

  return hours;
}

export function toLimitSeconds(enabled: boolean, hours: number | null): number | null | "invalid" {
  if (!enabled) {
    return null;
  }

  if (hours === null) {
    return "invalid";
  }

  const seconds = hoursToSeconds(hours);
  if (seconds < MIN_SCREEN_TIME_SECONDS || seconds > MAX_SCREEN_TIME_SECONDS) {
    return "invalid";
  }

  return seconds;
}
