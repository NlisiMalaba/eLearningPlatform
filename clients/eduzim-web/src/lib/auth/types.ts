export type TokenPair = {
  accessToken: string;
  accessTokenExpiresAt: Date;
  refreshToken: string;
  refreshTokenExpiresAt: Date;
};

export type SessionInfo = {
  authenticated: boolean;
  accessTokenExpiresAt?: string;
  userId?: string;
  role?: string;
  tenantId?: string;
};

export type PublicRegisterRole = "ParentGuardian" | "SchoolAdmin";

export const PUBLIC_REGISTER_ROLES: readonly PublicRegisterRole[] = [
  "ParentGuardian",
  "SchoolAdmin",
];
