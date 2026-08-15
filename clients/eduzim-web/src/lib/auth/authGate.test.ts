import { describe, expect, it } from "vitest";
import { decideAuthGate } from "@/lib/auth/authGate";

function jwtWithExp(expSeconds: number): string {
  const payload = Buffer.from(JSON.stringify({ exp: expSeconds }), "utf8").toString("base64url");
  return `header.${payload}.signature`;
}

const futureAccess = jwtWithExp(Math.floor(Date.now() / 1000) + 3_600);
const expiredAccess = jwtWithExp(Math.floor(Date.now() / 1000) - 60);

describe("decideAuthGate", () => {
  it("allows public paths without tokens", () => {
    expect(
      decideAuthGate({ pathname: "/~offline", accessToken: undefined, refreshToken: undefined }),
    ).toEqual({ type: "allow" });
  });

  it("sends expired sessions on app routes to login when refresh is missing", () => {
    expect(
      decideAuthGate({
        pathname: "/",
        accessToken: expiredAccess,
        refreshToken: undefined,
      }),
    ).toEqual({ type: "login" });
  });

  it("refreshes expired access tokens on app routes when a refresh cookie exists", () => {
    expect(
      decideAuthGate({
        pathname: "/",
        accessToken: expiredAccess,
        refreshToken: "refresh-token",
      }),
    ).toEqual({ type: "refresh", onward: "allow" });
  });

  it("redirects authenticated users away from login", () => {
    expect(
      decideAuthGate({
        pathname: "/login",
        accessToken: futureAccess,
        refreshToken: "refresh-token",
      }),
    ).toEqual({ type: "home" });
  });

  it("allows the login page when the session cannot be restored", () => {
    expect(
      decideAuthGate({
        pathname: "/login",
        accessToken: expiredAccess,
        refreshToken: undefined,
      }),
    ).toEqual({ type: "allow" });
  });
});
