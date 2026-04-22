import { describe, expect, it } from "vitest";
import { buildUserFromAccessToken, decodeJwtPayload } from "./jwt";

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

  it("builds a user from access token claims", () => {
    const token = createToken({
      sub: "user-2",
      email: "carlos.silva@lootera.com",
      role: "Costumer",
      email_verified: "false",
    });

    expect(buildUserFromAccessToken(token)).toEqual(
      expect.objectContaining({
        id: "user-2",
        email: "carlos.silva@lootera.com",
        role: "customer",
        emailVerified: false,
        name: "Carlos Silva",
      }),
    );
  });

  it("uses the name claim when present", () => {
    const token = createToken({
      sub: "user-3",
      name: "Maria Lootera",
      email: "maria@lootera.com",
      role: "Costumer",
      email_verified: "true",
    });

    expect(buildUserFromAccessToken(token)).toEqual(
      expect.objectContaining({
        name: "Maria Lootera",
      }),
    );
  });
});
