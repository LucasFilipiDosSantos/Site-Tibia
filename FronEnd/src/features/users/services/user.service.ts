import { getStoredUsers, saveStoredUsers } from "../../auth/utils/auth.storage";
import type { User } from "../types/user.types";

export const userService = {
  getUsers: (): User[] => {
    return getStoredUsers();
  },

  getUserById: (id: string): User | undefined => {
    return getStoredUsers().find(u => u.id === id);
  },

  getUserByEmail: (email: string): User | undefined => {
    return getStoredUsers().find(u => u.email === email);
  },

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
    const index = users.findIndex(u => u.id === id);
    if (index === -1) return undefined;
    users[index] = { ...users[index], ...updates };
    saveStoredUsers(users);
    return users[index];
  },
};