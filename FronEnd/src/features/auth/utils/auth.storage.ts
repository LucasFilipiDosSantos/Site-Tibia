import type { AuthSession, StoredUserMock } from "../types/auth.types";

const STORAGE_KEYS = {
  USERS: "lootera_mock_users",
  SESSION: "lootera_session",
} as const;

const safeParseJSON = <T>(value: string | null, fallback: T): T => {
  if (!value) return fallback;
  try {
    return JSON.parse(value);
  } catch {
    return fallback;
  }
};

export const getStoredUsers = (): StoredUserMock[] => {
  return safeParseJSON(localStorage.getItem(STORAGE_KEYS.USERS), []);
};

export const saveStoredUsers = (users: StoredUserMock[]): void => {
  localStorage.setItem(STORAGE_KEYS.USERS, JSON.stringify(users));
};

export const getStoredSession = (): AuthSession | null => {
  return safeParseJSON(localStorage.getItem(STORAGE_KEYS.SESSION), null);
};

export const saveStoredSession = (session: AuthSession | null): void => {
  if (session) {
    localStorage.setItem(STORAGE_KEYS.SESSION, JSON.stringify(session));
  } else {
    localStorage.removeItem(STORAGE_KEYS.SESSION);
  }
};

export const seedMockUsersIfNeeded = (): void => {
  const existing = getStoredUsers();
  if (existing.length > 0) return;

  const mockUsers: StoredUserMock[] = [
    { id: "1", name: "Carlos Silva", email: "carlos@email.com", role: "customer", createdAt: "2026-01-15T10:00:00Z", totalSpent: 169.60, ordersCount: 2, password: "password123" },
    { id: "2", name: "Ana Pereira", email: "ana@email.com", role: "customer", createdAt: "2026-02-20T14:00:00Z", totalSpent: 379.80, ordersCount: 2, password: "password123" },
    { id: "3", name: "Ricardo Santos", email: "ricardo@email.com", role: "customer", createdAt: "2026-03-10T09:00:00Z", totalSpent: 1599.90, ordersCount: 1, password: "password123" },
    { id: "admin", name: "Admin Lootera", email: "admin@lootera.com", role: "admin", createdAt: "2025-12-01T00:00:00Z", totalSpent: 0, ordersCount: 0, password: "admin123" },
  ];

  saveStoredUsers(mockUsers);
};