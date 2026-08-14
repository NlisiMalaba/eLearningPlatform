import { decodeJwtExpiry, readJwtRole, readJwtUserId } from "@/lib/auth/jwt";
import type { SessionInfo } from "@/lib/auth/types";

export function sessionInfoFromAccessToken(accessToken: string): SessionInfo {
  return {
    authenticated: true,
    accessTokenExpiresAt: decodeJwtExpiry(accessToken)?.toISOString(),
    userId: readJwtUserId(accessToken) ?? undefined,
    role: readJwtRole(accessToken) ?? undefined,
  };
}
