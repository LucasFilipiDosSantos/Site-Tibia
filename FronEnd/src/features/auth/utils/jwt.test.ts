import { describe, expect, it } from "vitest";
import { decodeJwtPayload } from "./jwt";

const createToken = (payload: Record<string, unknown>) => {
  const header = btoa(JSON.stringify({ alg: "HS256", typ: "JWT" }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.signature`;
};

describe("jwt utils", () => {
  it("decodes payload from JWT token", () => {
    const token = createToken({
      sub: "user-1",
      email: "admin@lootera.com",
      role: "Admin",
      email_verified: "true",
    });

    expect(decodeJwtPayload(token)).toMatchObject({
      sub: "user-1",
      email: "admin@lootera.com",
      role: "Admin",
      email_verified: "true",
    });
  });
});
