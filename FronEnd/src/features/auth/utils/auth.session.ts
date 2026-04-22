import type { AuthSession } from "../types/auth.types";

const AUTH_SESSION_KEY = "lootera_auth_session";

const canUseStorage = () => typeof window !== "undefined" && typeof window.localStorage !== "undefined";

export const getStoredAuthSession = (): AuthSession | null => {
  if (!canUseStorage()) {
    return null;
  }

  const raw = window.localStorage.getItem(AUTH_SESSION_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as AuthSession;
  } catch {
    window.localStorage.removeItem(AUTH_SESSION_KEY);
    return null;
  }
};

export const saveAuthSession = (session: AuthSession | null): void => {
  if (!canUseStorage()) {
    return;
  }

  if (session) {
    window.localStorage.setItem(AUTH_SESSION_KEY, JSON.stringify(session));
    return;
  }

  window.localStorage.removeItem(AUTH_SESSION_KEY);
};

export const isTokenExpired = (expiresAtIso: string, skewMs = 30_000): boolean => {
  const expiresAt = new Date(expiresAtIso).getTime();

  if (Number.isNaN(expiresAt)) {
    return true;
  }

  return expiresAt <= Date.now() + skewMs;
};
