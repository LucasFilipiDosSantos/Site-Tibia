const configuredApiUrl =
  import.meta.env.VITE_API_URL
  ?? import.meta.env.NEXT_PUBLIC_API_URL
  ?? (import.meta.env.PROD ? "/api" : "http://localhost:8080/api");

export const API_BASE_URL = configuredApiUrl.replace(/\/$/, "");

const getApiPublicBaseUrl = (apiBaseUrl: string): string => {
  if (apiBaseUrl === "/api") {
    return "";
  }

  if (apiBaseUrl.endsWith("/api")) {
    return apiBaseUrl.slice(0, -"/api".length);
  }

  return apiBaseUrl;
};

export const API_PUBLIC_BASE_URL = getApiPublicBaseUrl(API_BASE_URL);

export const buildApiAssetUrl = (path?: string | null): string | null => {
  if (!path) {
    return null;
  }

  if (/^https?:\/\//i.test(path) || path.startsWith("blob:") || path.startsWith("data:")) {
    return path;
  }

  if (path.startsWith("/uploads/")) {
    return `${API_PUBLIC_BASE_URL}${path}`;
  }

  return path;
};

export const normalizeApiAssetUrl = (url?: string | null): string | null => {
  if (!url) {
    return null;
  }

  if (API_PUBLIC_BASE_URL && url.startsWith(`${API_PUBLIC_BASE_URL}/uploads/`)) {
    return url.slice(API_PUBLIC_BASE_URL.length);
  }

  if (url.startsWith(`${API_BASE_URL}/uploads/`)) {
    return url.slice(API_BASE_URL.length);
  }

  if (url.startsWith("/api/uploads/")) {
    return url.slice("/api".length);
  }

  return url;
};
