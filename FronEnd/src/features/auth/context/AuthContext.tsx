import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { toast } from "sonner";
import { authService } from "../services/auth.service";
import type { AuthUser } from "../types/auth.types";

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
    let mounted = true;

    const restore = async () => {
      try {
        authService.clearLegacyAuthStorage();
        const currentUser = await authService.restoreSession();
        if (mounted) {
          setUser(currentUser);
        }
      } catch (error) {
        authService.clearSession();
        if (mounted) {
          setUser(null);
          toast.error(error instanceof Error ? error.message : "Nao foi possivel restaurar sua sessao.");
        }
      } finally {
        if (mounted) {
          setIsLoading(false);
        }
      }
    };

    void restore();

    return () => {
      mounted = false;
    };
  }, []);

  const login = async (email: string, password: string): Promise<boolean> => {
    try {
      const currentUser = await authService.login({ email, password });
      setUser(currentUser);
      toast.success(`Bem-vindo, ${currentUser.name}!`);
      return true;
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel entrar.");
      return false;
    }
  };

  const register = async (name: string, email: string, password: string): Promise<boolean> => {
    try {
      await authService.register({ name, email, password });
      setUser(await authService.login({ email, password }));
      toast.success("Conta criada com sucesso!");
      return true;
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Nao foi possivel criar a conta.");
      return false;
    }
  };

  const logout = () => {
    authService.clearSession();
    setUser(null);
    toast.info("Voce saiu da conta");
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isAdmin: user?.role === "admin",
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within AuthProvider");
  }

  return ctx;
};
