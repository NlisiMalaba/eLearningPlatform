export function safeRedirectPath(from: string | null | undefined): string {
  if (!from || !from.startsWith("/") || from.startsWith("//") || from.startsWith("/\\")) {
    return "/";
  }

  if (from.startsWith("/login") || from.startsWith("/register") || from.startsWith("/sso")) {
    return "/";
  }

  return from;
}
