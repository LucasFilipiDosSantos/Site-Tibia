import { createContext, useContext, useState, useCallback, type ReactNode } from "react";
import { toast } from "sonner";
import { users as mockUsers, type User } from "@/data/mockData";

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  login: (email: string, password: string) => boolean;
  register: (name: string, email: string, password: string) => boolean;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);

  const login = useCallback((email: string, _password: string) => {
    const found = mockUsers.find((u) => u.email === email);
    if (found) {
      setUser(found);
      toast.success(`Bem-vindo, ${found.name}!`);
      return true;
    }
    // Demo: any email works
    const demo: User = { id: "demo", name: email.split("@")[0], email, role: "customer", createdAt: new Date().toISOString(), totalSpent: 0, ordersCount: 0 };
    setUser(demo);
    toast.success(`Bem-vindo, ${demo.name}!`);
    return true;
  }, []);

  const register = useCallback((name: string, email: string, _password: string) => {
    const newUser: User = { id: crypto.randomUUID(), name, email, role: "customer", createdAt: new Date().toISOString(), totalSpent: 0, ordersCount: 0 };
    setUser(newUser);
    toast.success("Conta criada com sucesso!");
    return true;
  }, []);

  const logout = useCallback(() => {
    setUser(null);
    toast.info("Você saiu da conta");
  }, []);

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, isAdmin: user?.role === "admin", login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
};
