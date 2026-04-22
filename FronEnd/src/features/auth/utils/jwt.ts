import type { AuthUser } from "../types/auth.types";

type JwtPayload = {
  sub?: string;
  name?: string;
  email?: string;
  role?: string;
  email_verified?: string;
};

const decodeBase64Url = (value: string): string => {
  const normalized = value.replace(/-/g, "+").replace(/_/g, "/");
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, "=");
  return atob(padded);
};

const prettifyNameFromEmail = (email: string): string => {
  const base = email.split("@")[0] ?? "Cliente";

  return base
    .split(/[._-]+/)
    .filter(Boolean)
    .map((chunk) => chunk.charAt(0).toUpperCase() + chunk.slice(1))
    .join(" ");
};

export const decodeJwtPayload = (token: string): JwtPayload | null => {
  const [, payload] = token.split(".");
  if (!payload) {
    return null;
  }

  try {
    return JSON.parse(decodeBase64Url(payload)) as JwtPayload;
  } catch {
    return null;
  }
};

export const buildUserFromAccessToken = (accessToken: string, previousUser?: AuthUser | null): AuthUser | null => {
  const payload = decodeJwtPayload(accessToken);

  if (!payload?.sub || !payload.email) {
    return null;
  }

  return {
    id: payload.sub,
    name: payload.name?.trim() || previousUser?.name || prettifyNameFromEmail(payload.email),
    email: payload.email,
    role: payload.role === "Admin" ? "admin" : "customer",
    createdAt: previousUser?.createdAt || new Date().toISOString(),
    totalSpent: previousUser?.totalSpent ?? 0,
    ordersCount: previousUser?.ordersCount ?? 0,
    emailVerified: payload.email_verified === "true",
  };
};
