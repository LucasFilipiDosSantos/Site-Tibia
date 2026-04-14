import { AUTH_MODE } from "../types/auth.types";
import type { AuthResult, AuthUser, LoginInput, RegisterInput, AuthSession } from "../types/auth.types";
import { validateLoginInput, validateRegisterInput, normalizeEmail } from "../utils/auth.validators";
import { getStoredUsers, saveStoredUsers, getStoredSession, saveStoredSession, seedMockUsersIfNeeded } from "../utils/auth.storage";

export const mockAuthService = {
  async login(input: LoginInput): Promise<AuthResult<AuthUser>> {
    if (AUTH_MODE !== "mock") throw new Error("Mock auth only");

    const validationError = validateLoginInput(input);
    if (validationError) return { success: false, error: validationError };

    seedMockUsersIfNeeded();
    const users = getStoredUsers();
    const normalizedEmail = normalizeEmail(input.email);
    const user = users.find(u => normalizeEmail(u.email) === normalizedEmail);

    if (!user || user.password !== input.password) {
      return { success: false, error: "E-mail ou senha inválidos" };
    }

    const authUser: AuthUser = {
      id: user.id,
      name: user.name,
      email: user.email,
      role: user.role,
      createdAt: user.createdAt,
      totalSpent: user.totalSpent,
      ordersCount: user.ordersCount,
    };

    const session: AuthSession = { user: authUser };
    saveStoredSession(session);

    return { success: true, data: authUser };
  },

  async register(input: RegisterInput): Promise<AuthResult<AuthUser>> {
    if (AUTH_MODE !== "mock") throw new Error("Mock auth only");

    const validationError = validateRegisterInput(input);
    if (validationError) return { success: false, error: validationError };

    seedMockUsersIfNeeded();
    const users = getStoredUsers();
    const normalizedEmail = normalizeEmail(input.email);

    if (users.some(u => normalizeEmail(u.email) === normalizedEmail)) {
      return { success: false, error: "E-mail já cadastrado" };
    }

    const newUser: AuthUser = {
      id: crypto.randomUUID(),
      name: input.name,
      email: input.email,
      role: "customer",
      createdAt: new Date().toISOString(),
      totalSpent: 0,
      ordersCount: 0,
    };

    const storedUser = { ...newUser, password: input.password };
    users.push(storedUser);
    saveStoredUsers(users);

    const session: AuthSession = { user: newUser };
    saveStoredSession(session);

    return { success: true, data: newUser };
  },

  logout(): void {
    saveStoredSession(null);
  },

  initializeAuth(): AuthSession | null {
    seedMockUsersIfNeeded();
    return getStoredSession();
  },
};