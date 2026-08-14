import { isAuthPath, isPublicPath } from "@/lib/auth/routes";
import { isJwtExpired } from "@/lib/auth/jwt";

export type AuthGateDecision =
  | { type: "allow" }
  | { type: "refresh"; onward: "allow" | "home" }
  | { type: "home" }
  | { type: "login" };

export function decideAuthGate(input: {
  pathname: string;
  accessToken: string | undefined;
  refreshToken: string | undefined;
  nowMs?: number;
}): AuthGateDecision {
  if (isPublicPath(input.pathname)) {
    return { type: "allow" };
  }

  const accessValid = Boolean(
    input.accessToken && !isJwtExpired(input.accessToken, input.nowMs),
  );
  const canRefresh = Boolean(input.refreshToken);

  if (isAuthPath(input.pathname)) {
    if (accessValid) {
      return { type: "home" };
    }

    if (canRefresh) {
      return { type: "refresh", onward: "home" };
    }

    return { type: "allow" };
  }

  if (accessValid) {
    return { type: "allow" };
  }

  if (canRefresh) {
    return { type: "refresh", onward: "allow" };
  }

  return { type: "login" };
}
