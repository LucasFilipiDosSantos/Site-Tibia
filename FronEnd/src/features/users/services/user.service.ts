import type { User } from "../types/user.types";

const USER_STORAGE_KEY = "lootera_users_cache";

const getStoredUsers = (): User[] => {
  if (typeof window === "undefined") {
    return [];
  }

  const raw = window.localStorage.getItem(USER_STORAGE_KEY);
  if (!raw) {
    return [];
  }

  try {
    return JSON.parse(raw) as User[];
  } catch {
    window.localStorage.removeItem(USER_STORAGE_KEY);
    return [];
  }
};

const saveStoredUsers = (users: User[]): void => {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(users));
};

export const userService = {
  getUsers: (): User[] => getStoredUsers(),

  getUserById: (id: string): User | undefined => getStoredUsers().find((user) => user.id === id),

  getUserByEmail: (email: string): User | undefined => getStoredUsers().find((user) => user.email === email),

  createUser: (user: Omit<User, "id" | "createdAt">): User => {
    const newUser: User = {
      ...user,
      id: crypto.randomUUID(),
      createdAt: new Date().toISOString(),
    };

    const users = getStoredUsers();
    users.push(newUser);
    saveStoredUsers(users);
    return newUser;
  },

  updateUser: (id: string, updates: Partial<User>): User | undefined => {
    const users = getStoredUsers();
    const index = users.findIndex((user) => user.id === id);

    if (index === -1) {
      return undefined;
    }

    users[index] = { ...users[index], ...updates };
    saveStoredUsers(users);
    return users[index];
  },
};
