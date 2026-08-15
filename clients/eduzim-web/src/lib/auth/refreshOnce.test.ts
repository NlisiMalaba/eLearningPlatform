import { describe, expect, it } from "vitest";
import { refreshSessionOnce } from "@/lib/auth/refreshOnce";

describe("refreshSessionOnce", () => {
  it("shares a single in-flight refresh", async () => {
    let calls = 0;
    const refresh = () => {
      calls += 1;
      return new Promise((resolve) => {
        setTimeout(resolve, 20);
      });
    };

    const [first, second] = await Promise.all([
      refreshSessionOnce(refresh),
      refreshSessionOnce(refresh),
    ]);

    expect(first).toBe(true);
    expect(second).toBe(true);
    expect(calls).toBe(1);
  });
});
