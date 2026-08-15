const SESSION_ID =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function isClassroomSessionId(value: string): boolean {
  return SESSION_ID.test(value.trim());
}

export function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

export function readField(raw: Record<string, unknown>, camel: string, pascal: string): unknown {
  return raw[camel] ?? raw[pascal];
}

export function readGuid(value: unknown): string | undefined {
  if (typeof value === "string" && isClassroomSessionId(value)) {
    return value;
  }

  return undefined;
}

export function readBoolean(value: unknown, fallback = true): boolean {
  return typeof value === "boolean" ? value : fallback;
}
