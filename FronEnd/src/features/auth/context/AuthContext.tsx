import { createContext, useContext, useState, useEffect, type ReactNode } from "react";
import { toast } from "sonner";
import type { AuthUser, AuthResult } from "../types/auth.types";
import { mockAuthService } from "../services/mock-auth.service";

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<boolean>;
  register: (name: string, email: string, password: string) => Promise<boolean>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const session = mockAuthService.initializeAuth();
    setUser(session?.user ?? null);
    setIsLoading(false);
  }, []);

  const login = async (email: string, password: string): Promise<boolean> => {
    const result: AuthResult<AuthUser> = await mockAuthService.login({ email, password });
    if (result.success) {
      setUser(result.data);
      toast.success(`Bem-vindo, ${result.data.name}!`);
      return true;
    } else {
      toast.error(result.error);
      return false;
    }
  };

  const register = async (name: string, email: string, password: string): Promise<boolean> => {
    const result: AuthResult<AuthUser> = await mockAuthService.register({ name, email, password });
    if (result.success) {
      setUser(result.data);
      toast.success("Conta criada com sucesso!");
      return true;
    } else {
      toast.error(result.error);
      return false;
    }
  };

  const logout = () => {
    mockAuthService.logout();
    setUser(null);
    toast.info("Você saiu da conta");
  };

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated: !!user,
      isAdmin: user?.role === "admin",
      isLoading,
      login,
      register,
      logout
    }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
};