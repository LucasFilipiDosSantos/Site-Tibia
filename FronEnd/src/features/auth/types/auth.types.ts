export type AuthUser = {
  id: string;
  name: string;
  email: string;
  role: "customer" | "admin";
  createdAt: string;
  totalSpent: number;
  ordersCount: number;
  emailVerified: boolean;
};

export type AuthSession = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
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

export type AuthTokens = {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
  refreshTokenExpiresAtUtc: string;
};

export type AuthApiResponse = AuthTokens;
