import { API_BASE_URL } from "@/lib/api-base-url";

type ApiRequestOptions = RequestInit & {
  auth?: boolean;
  retryOnUnauthorized?: boolean;
};

type ApiClientOptions = RequestInit & {
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

const buildHeaders = (headers?: HeadersInit): Headers => {
  const nextHeaders = new Headers(headers);

  if (!nextHeaders.has("Content-Type")) {
    nextHeaders.set("Content-Type", "application/json");
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

const request = async <T>(path: string, options: ApiClientOptions = {}): Promise<T> => {
  const { retryOnUnauthorized = true, headers, ...rest } = options;
  const nextHeaders = buildHeaders(headers);
  if (rest.body instanceof FormData) {
    nextHeaders.delete("Content-Type");
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    credentials: "include",
    ...rest,
    headers: nextHeaders,
  });

  if (response.status === 401 && retryOnUnauthorized && path !== "/auth/refresh" && path !== "/auth/login") {
    try {
      await request("/auth/refresh", { method: "POST", retryOnUnauthorized: false });
      return request<T>(path, { ...options, retryOnUnauthorized: false });
    } catch {
      window.dispatchEvent(new CustomEvent("lootera:auth:unauthorized"));
      throw new ApiError("Sua sessao expirou. Faca login novamente.", 401);
    }
  }

  if (!response.ok) {
    if (response.status === 401) {
      window.dispatchEvent(new CustomEvent("lootera:auth:unauthorized"));
    }

    if (response.status === 403) {
      window.dispatchEvent(new CustomEvent("lootera:auth:forbidden"));
    }

    throw new ApiError(await parseErrorMessage(response), response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
};

export const apiClient = {
  request,

  get<T>(path: string, options?: ApiClientOptions): Promise<T> {
    return request<T>(path, { ...options, method: "GET" });
  },

  post<T>(path: string, body?: unknown, options?: ApiClientOptions): Promise<T> {
    return request<T>(path, {
      ...options,
      method: "POST",
      body: body instanceof FormData ? body : body === undefined ? undefined : JSON.stringify(body),
    });
  },

  put<T>(path: string, body?: unknown, options?: ApiClientOptions): Promise<T> {
    return request<T>(path, {
      ...options,
      method: "PUT",
      body: body instanceof FormData ? body : body === undefined ? undefined : JSON.stringify(body),
    });
  },

  delete<T>(path: string, options?: ApiClientOptions): Promise<T> {
    return request<T>(path, { ...options, method: "DELETE" });
  },
};

export const apiRequest = async <T>(path: string, options: ApiRequestOptions = {}): Promise<T> => {
  const { auth: _auth, ...rest } = options;
  return apiClient.request<T>(path, rest);
};
