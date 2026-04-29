import { API_BASE_URL } from "@/lib/api-base-url";
import type { AuthMeResponse, AuthUser, LoginInput, RegisterInput } from "../types/auth.types";

const LEGACY_AUTH_SESSION_KEY = "lootera_auth_session";

const clearLegacyAuthStorage = (): void => {
  if (typeof window === "undefined" || !window.localStorage) {
    return;
  }

  window.localStorage.removeItem(LEGACY_AUTH_SESSION_KEY);
};

const getErrorMessage = async (response: Response, fallback: string): Promise<string> => {
  try {
    const body = await response.json() as {
      detail?: string;
      title?: string;
      message?: string;
      errors?: Record<string, string[]>;
    };

    if (body.detail) {
      return body.detail;
    }

    if (body.message) {
      return body.message;
    }

    if (body.title) {
      return body.title;
    }

    const firstError = Object.values(body.errors ?? {})[0]?.[0];
    return firstError ?? fallback;
  } catch {
    return fallback;
  }
};

const postJson = async <TResponse>(path: string, body: unknown): Promise<TResponse> => {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, "Nao foi possivel completar a operacao."));
  }

  return response.json() as Promise<TResponse>;
};

const getJson = async <TResponse>(path: string): Promise<TResponse> => {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    credentials: "include",
  });

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, "Nao foi possivel validar sua sessao."));
  }

  return response.json() as Promise<TResponse>;
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
    await postJson<{ message: string }>("/auth/register", {
      name: input.name,
      email: input.email,
      password: input.password,
    });
  },

  async login(input: LoginInput): Promise<AuthUser> {
    await postJson<AuthMeResponse>("/auth/login", input);
    return this.getCurrentUser();
  },

  async refresh(): Promise<AuthUser> {
    return toUser(await postJson<AuthMeResponse>("/auth/refresh", undefined));
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
    return toUser(await getJson<AuthMeResponse>("/auth/me"));
  },

  clearSession(): void {
    clearLegacyAuthStorage();
    void postJson<void>("/auth/logout", undefined).catch(() => undefined);
  },
};
