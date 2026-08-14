export const AUTH_PATHS = ["/login", "/register", "/verify-email", "/sso"] as const;

export const PUBLIC_PATHS = ["/~offline"] as const;

export function isAuthPath(pathname: string): boolean {
  return AUTH_PATHS.some(
    (path) => pathname === path || pathname.startsWith(`${path}/`),
  );
}

export function isPublicPath(pathname: string): boolean {
  if (pathname.startsWith("/api/")) {
    return true;
  }

  if (pathname.startsWith("/_next/") || pathname.startsWith("/icons/")) {
    return true;
  }

  if (
    pathname === "/sw.js" ||
    pathname.startsWith("/swe-worker") ||
    pathname === "/manifest.webmanifest" ||
    pathname === "/favicon.ico"
  ) {
    return true;
  }

  return PUBLIC_PATHS.some(
    (path) => pathname === path || pathname.startsWith(`${path}/`),
  );
}
