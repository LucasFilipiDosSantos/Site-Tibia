export type AuthUser = {
  id: string;
  name: string;
  email: string;
  role: "customer" | "admin";
  createdAt: string;
  totalSpent: number;
  ordersCount: number;
};

export type StoredUserMock = AuthUser & {
  password: string; // MOCK ONLY - Never use in production
};

export type AuthSession = {
  user: AuthUser;
};

export type LoginInput = {
  email: string;
  password: string;
};

export type RegisterInput = {
  name: string;
  email: string;
  password: string;
};

export type AuthResult<T = void> =
  | { success: true; data: T }
  | { success: false; error: string };

export const AUTH_MODE = "mock" as const;