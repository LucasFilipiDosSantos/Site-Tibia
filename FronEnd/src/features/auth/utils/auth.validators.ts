import type { LoginInput, RegisterInput } from "../types/auth.types";

export const normalizeEmail = (email: string): string => {
  return email.toLowerCase().trim();
};

export const isValidEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

export const validateLoginInput = (input: LoginInput): string | null => {
  if (!input.email.trim()) return "E-mail é obrigatório";
  if (!isValidEmail(input.email)) return "E-mail inválido";
  if (!input.password.trim()) return "Senha é obrigatória";
  return null;
};

export const validateRegisterInput = (input: RegisterInput): string | null => {
  if (!input.name.trim()) return "Nome é obrigatório";
  if (!input.email.trim()) return "E-mail é obrigatório";
  if (!isValidEmail(input.email)) return "E-mail inválido";
  if (!input.password.trim()) return "Senha é obrigatória";
  if (input.password.length < 10) return "Senha deve ter pelo menos 10 caracteres";
  if (!/[A-Z]/.test(input.password)) return "Senha deve ter pelo menos uma letra maiuscula";
  if (!/[a-z]/.test(input.password)) return "Senha deve ter pelo menos uma letra minuscula";
  if (!/[0-9]/.test(input.password)) return "Senha deve ter pelo menos um numero";
  if (!/[^A-Za-z0-9]/.test(input.password)) return "Senha deve ter pelo menos um caractere especial";
  return null;
};
