import { authService } from "@/features/auth/services/auth.service";
import { getStoredAuthSession } from "@/features/auth/utils/auth.session";
import { API_BASE_URL } from "@/lib/api-base-url";

type ApiRequestOptions = RequestInit & {
  auth?: boolean;
  retryOnUnauthorized?: boolean;
};

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

const buildHeaders = (headers?: HeadersInit, auth = false): Headers => {
  const nextHeaders = new Headers(headers);

  if (!nextHeaders.has("Content-Type")) {
    nextHeaders.set("Content-Type", "application/json");
  }

  if (auth) {
    const session = getStoredAuthSession();
    if (session?.accessToken) {
      nextHeaders.set("Authorization", `Bearer ${session.accessToken}`);
    }
  }

  return nextHeaders;
};

const parseErrorMessage = async (response: Response): Promise<string> => {
  try {
    const body = await response.json() as { detail?: string; title?: string; message?: string };
    return body.detail || body.message || body.title || "Nao foi possivel completar a solicitacao.";
  } catch {
    return "Nao foi possivel completar a solicitacao.";
  }
};

export const apiRequest = async <T>(path: string, options: ApiRequestOptions = {}): Promise<T> => {
  const { auth = false, retryOnUnauthorized = auth, headers, ...rest } = options;

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...rest,
    headers: buildHeaders(headers, auth),
  });

  if (response.status === 401 && auth && retryOnUnauthorized) {
    try {
      await authService.refresh();
      return apiRequest<T>(path, { ...options, retryOnUnauthorized: false });
    } catch {
      authService.clearSession();
      throw new ApiError("Sua sessao expirou. Faca login novamente.", 401);
    }
  }

  if (!response.ok) {
    throw new ApiError(await parseErrorMessage(response), response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
};
