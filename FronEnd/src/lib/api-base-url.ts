const configuredApiUrl =
  import.meta.env.NEXT_PUBLIC_API_URL
  ?? import.meta.env.VITE_API_BASE_URL
  ?? (import.meta.env.PROD ? "/api" : "http://localhost:8080/api");

export const API_BASE_URL = configuredApiUrl.replace(/\/$/, "");
