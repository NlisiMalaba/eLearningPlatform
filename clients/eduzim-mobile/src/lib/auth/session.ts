import { readJwtRole, readJwtUserId } from "@/lib/auth/jwt";

let accessToken: string | null = null;

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function getAccessToken(): string | null {
  return accessToken;
}

export function getStudentId(): string | null {
  if (!accessToken) {
    return null;
  }

  const role = readJwtRole(accessToken);
  if (role && role !== "Student") {
    return null;
  }

  return readJwtUserId(accessToken);
}
