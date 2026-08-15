import { apiFetch } from "@/lib/api/client";
import { toColorInputValue } from "@/lib/admin/branding";

export const SUBSCRIPTION_STATUSES = ["Active", "GracePeriod", "Suspended", "Cancelled"] as const;

export type SubscriptionStatus = (typeof SUBSCRIPTION_STATUSES)[number];

export type TenantDashboard = {
  enrolledStudentsCount: number;
  activeTeachersCount: number;
  subscriptionStatus: SubscriptionStatus | null;
  storageUsageBytes: number;
};

export type TenantDetails = {
  id: string;
  name: string;
  schoolName: string;
  primaryColour: string;
  logoUrl: string | null;
};

export async function getTenantDashboard(tenantId: string): Promise<TenantDashboard> {
  const raw = await apiFetch<Record<string, unknown>>(`/api/v1/tenants/${tenantId}/dashboard`);
  return mapDashboard(raw);
}

export async function getTenantDetails(tenantId: string): Promise<TenantDetails> {
  const raw = await apiFetch<Record<string, unknown>>(`/api/v1/tenants/${tenantId}`);
  return mapDetails(raw);
}

export async function updateBranding(
  tenantId: string,
  input: { schoolName: string; primaryColour: string },
): Promise<void> {
  await apiFetch(`/api/v1/tenants/${tenantId}/branding`, {
    method: "PUT",
    body: JSON.stringify({
      schoolName: input.schoolName,
      primaryColour: input.primaryColour,
    }),
  });
}

export async function uploadBrandingLogo(tenantId: string, file: File): Promise<string> {
  const body = new FormData();
  body.append("file", file);
  const raw = await apiFetch<Record<string, unknown>>(`/api/v1/tenants/${tenantId}/branding/logo`, {
    method: "POST",
    body,
  });
  return readString(raw.logoUrl) ?? "";
}

export function mapDashboard(raw: Record<string, unknown>): TenantDashboard {
  return {
    enrolledStudentsCount: readNumber(raw.enrolledStudentsCount),
    activeTeachersCount: readNumber(raw.activeTeachersCount),
    subscriptionStatus: parseSubscriptionStatus(raw.subscriptionStatus),
    storageUsageBytes: readNumber(raw.storageUsageBytes),
  };
}

function mapDetails(raw: Record<string, unknown>): TenantDetails {
  const branding =
    raw.branding && typeof raw.branding === "object" ? (raw.branding as Record<string, unknown>) : {};
  return {
    id: readString(raw.id) ?? "",
    name: readString(raw.name) ?? "",
    schoolName: readString(branding.schoolName) ?? readString(raw.name) ?? "",
    primaryColour: toColorInputValue(readString(branding.primaryColour) ?? "#1976D2"),
    logoUrl: readString(branding.logoUrl) ?? null,
  };
}

function parseSubscriptionStatus(value: unknown): SubscriptionStatus | null {
  if (typeof value === "number" && SUBSCRIPTION_STATUSES[value]) {
    return SUBSCRIPTION_STATUSES[value];
  }

  if (typeof value === "string" && (SUBSCRIPTION_STATUSES as readonly string[]).includes(value)) {
    return value as SubscriptionStatus;
  }

  return null;
}

function readString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

function readNumber(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}
