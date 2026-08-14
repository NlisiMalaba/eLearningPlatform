export const CONTENT_MANAGER_ROLES = ["Teacher", "SchoolAdmin", "PlatformAdmin"] as const;

export type ContentManagerRole = (typeof CONTENT_MANAGER_ROLES)[number];

export function canManageSchoolContent(role: string | undefined): boolean {
  return role === "Teacher" || role === "SchoolAdmin" || role === "PlatformAdmin";
}
