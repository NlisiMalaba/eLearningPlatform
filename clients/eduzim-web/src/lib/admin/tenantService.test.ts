import { describe, expect, it } from "vitest";
import { mapDashboard } from "@/lib/admin/tenantService";

describe("tenant dashboard mapping", () => {
  it("maps enrolment, teachers, subscription, and storage", () => {
    const dto = mapDashboard({
      enrolledStudentsCount: 40,
      activeTeachersCount: 6,
      subscriptionStatus: 0,
      storageUsageBytes: 1024,
    });
    expect(dto.enrolledStudentsCount).toBe(40);
    expect(dto.activeTeachersCount).toBe(6);
    expect(dto.subscriptionStatus).toBe("Active");
    expect(dto.storageUsageBytes).toBe(1024);
  });
});
