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

export type LoginInput = {
  email: string;
  password: string;
};

export type RegisterInput = {
  name: string;
  email: string;
  password: string;
};

export type AuthMeResponse = {
  id?: string;
  Id?: string;
  userId?: string;
  UserId?: string;
  name?: string;
  Name?: string;
  email?: string;
  Email?: string;
  role?: string;
  Role?: string;
  emailVerified?: boolean;
  EmailVerified?: boolean;
  createdAtUtc?: string;
  CreatedAtUtc?: string;
};
