type JwtPayload = {
  sub?: string;
  name?: string;
  email?: string;
  role?: string;
  email_verified?: string;
};

const decodeBase64Url = (value: string): string => {
  const normalized = value.replace(/-/g, "+").replace(/_/g, "/");
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, "=");
  return atob(padded);
};

export const decodeJwtPayload = (token: string): JwtPayload | null => {
  const [, payload] = token.split(".");
  if (!payload) {
    return null;
  }

  try {
    return JSON.parse(decodeBase64Url(payload)) as JwtPayload;
  } catch {
    return null;
  }
};
