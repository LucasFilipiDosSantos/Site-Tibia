import { apiClient } from "@/lib/api";
import type { AuthMeResponse, AuthUser, LoginInput, RegisterInput } from "../types/auth.types";

const LEGACY_AUTH_SESSION_KEY = "lootera_auth_session";

const clearLegacyAuthStorage = (): void => {
  if (typeof window === "undefined" || !window.localStorage) {
    return;
  }

  window.localStorage.removeItem(LEGACY_AUTH_SESSION_KEY);
};

const toUser = (payload: AuthMeResponse): AuthUser => {
  const id = payload.id ?? payload.Id ?? payload.userId ?? payload.UserId ?? "";
  const name = payload.name ?? payload.Name ?? "";
  const email = payload.email ?? payload.Email ?? "";
  const role = payload.role ?? payload.Role ?? "Customer";

  if (!id || !name || !email) {
    throw new Error("Resposta de sessao invalida.");
  }

  return {
    id,
    name,
    email,
    role: role === "Admin" || role === "admin" ? "admin" : "customer",
    createdAt: payload.createdAtUtc ?? payload.CreatedAtUtc ?? new Date().toISOString(),
    totalSpent: 0,
    ordersCount: 0,
    emailVerified: payload.emailVerified ?? payload.EmailVerified ?? false,
  };
};

export const authService = {
  clearLegacyAuthStorage,

  async register(input: RegisterInput): Promise<void> {
    await apiClient.post<{ message: string }>("/auth/register", {
      name: input.name,
      email: input.email,
      password: input.password,
    });
  },

  async login(input: LoginInput): Promise<AuthUser> {
    await apiClient.post<AuthMeResponse>("/auth/login", input);
    return this.getCurrentUser();
  },

  async refresh(): Promise<AuthUser> {
    return toUser(await apiClient.post<AuthMeResponse>("/auth/refresh", undefined, { retryOnUnauthorized: false }));
  },

  async restoreSession(): Promise<AuthUser | null> {
    try {
      return await this.getCurrentUser();
    } catch {
      try {
        return await this.refresh();
      } catch {
        return null;
      }
    }
  },

  async getCurrentUser(): Promise<AuthUser> {
    return toUser(await apiClient.get<AuthMeResponse>("/auth/me", { retryOnUnauthorized: false }));
  },

  clearSession(): void {
    clearLegacyAuthStorage();
    void apiClient.post<void>("/auth/logout", undefined, { retryOnUnauthorized: false }).catch(() => undefined);
  },
};
