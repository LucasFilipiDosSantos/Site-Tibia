import type { AuthApiResponse, AuthSession, AuthTokens, LoginInput, RegisterInput } from "../types/auth.types";
import { getStoredAuthSession, isTokenExpired, saveAuthSession } from "../utils/auth.session";
import { buildUserFromAccessToken } from "../utils/jwt";

const API_BASE_URL = (
  import.meta.env.NEXT_PUBLIC_API_URL
  ?? import.meta.env.VITE_API_BASE_URL
  ?? (import.meta.env.PROD ? "/api" : "http://localhost:8080/api")
).replace(/\/$/, "");

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
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    throw new Error(await getErrorMessage(response, "Nao foi possivel completar a operacao."));
  }

  return response.json() as Promise<TResponse>;
};

const extractTokens = (payload: AuthApiResponse): AuthTokens => ({
  accessToken: payload.accessToken,
  refreshToken: payload.refreshToken,
  accessTokenExpiresAtUtc: payload.accessTokenExpiresAtUtc,
  refreshTokenExpiresAtUtc: payload.refreshTokenExpiresAtUtc,
});

const toSession = (tokens: AuthTokens, previousSession?: AuthSession | null): AuthSession => {
  const user = buildUserFromAccessToken(tokens.accessToken, previousSession?.user);

  if (!user) {
    throw new Error("Nao foi possivel identificar o usuario autenticado.");
  }

  return {
    ...tokens,
    user,
  };
};

export const authService = {
  getStoredSession(): AuthSession | null {
    return getStoredAuthSession();
  },

  async register(input: RegisterInput): Promise<void> {
    await postJson<{ message: string }>("/auth/register", {
      name: input.name,
      email: input.email,
      password: input.password,
    });
  },

  async login(input: LoginInput): Promise<AuthSession> {
    const payload = await postJson<AuthApiResponse>("/auth/login", input);
    const session = toSession(extractTokens(payload), getStoredAuthSession());
    saveAuthSession(session);
    return session;
  },

  async refresh(refreshToken?: string): Promise<AuthSession> {
    const currentSession = getStoredAuthSession();
    const nextRefreshToken = refreshToken ?? currentSession?.refreshToken;

    if (!nextRefreshToken) {
      throw new Error("Sessao expirada. Faca login novamente.");
    }

    const payload = await postJson<AuthApiResponse>("/auth/refresh", {
      refreshToken: nextRefreshToken,
    });

    const session = toSession(extractTokens(payload), currentSession);
    saveAuthSession(session);
    return session;
  },

  async restoreSession(): Promise<AuthSession | null> {
    const session = getStoredAuthSession();
    if (!session) {
      return null;
    }

    if (isTokenExpired(session.refreshTokenExpiresAtUtc)) {
      saveAuthSession(null);
      return null;
    }

    if (!isTokenExpired(session.accessTokenExpiresAtUtc)) {
      return session;
    }

    return this.refresh(session.refreshToken);
  },

  clearSession(): void {
    saveAuthSession(null);
  },
};
