import { cookies } from "next/headers";
import { ACCESS_TOKEN_COOKIE } from "@/lib/auth/cookies";
import { isJwtExpired } from "@/lib/auth/jwt";
import { sessionInfoFromAccessToken } from "@/lib/auth/sessionInfo";
import type { SessionInfo } from "@/lib/auth/types";

export async function readServerSession(): Promise<SessionInfo | null> {
  const cookieStore = await cookies();
  const accessToken = cookieStore.get(ACCESS_TOKEN_COOKIE)?.value;
  if (!accessToken || isJwtExpired(accessToken)) {
    return null;
  }

  return sessionInfoFromAccessToken(accessToken);
}
