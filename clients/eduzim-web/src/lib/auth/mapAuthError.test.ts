import { describe, expect, it } from "vitest";
import { ApiError, NetworkError } from "@/lib/api/errors";
import { loginErrorKey, registerErrorKey } from "@/lib/auth/mapAuthError";

describe("loginErrorKey", () => {
  it("maps lockout, unverified, and invalid credentials", () => {
    expect(loginErrorKey(new ApiError(403, "Account is locked out."))).toBe("auth.error.locked");
    expect(loginErrorKey(new ApiError(403, "Email address must be confirmed."))).toBe(
      "auth.error.unverified",
    );
    expect(loginErrorKey(new ApiError(401, "Unauthorized"))).toBe("auth.error.invalidCredentials");
    expect(loginErrorKey(new NetworkError())).toBe("auth.error.network");
  });
});

describe("registerErrorKey", () => {
  it("maps duplicate email conflicts", () => {
    expect(registerErrorKey(new ApiError(409, "already registered"))).toBe("auth.error.conflict");
  });
});
