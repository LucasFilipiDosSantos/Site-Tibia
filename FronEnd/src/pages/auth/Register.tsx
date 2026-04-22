import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Check, Eye, EyeOff } from "lucide-react";
import PublicLayout from "@/components/lootera/PublicLayout";
import { useAuth } from "@/features/auth/context/AuthContext";
import logoImg from "@/assets/lootera-logo.png";

const Register = () => {
  const { register, isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const passwordRequirements = [
    { label: "Minimo de 10 caracteres", isMet: password.length >= 10 },
    { label: "Uma letra maiuscula", isMet: /[A-Z]/.test(password) },
    { label: "Uma letra minuscula", isMet: /[a-z]/.test(password) },
    { label: "Um numero", isMet: /[0-9]/.test(password) },
    { label: "Um caractere especial", isMet: /[^A-Za-z0-9]/.test(password) }
  ];

  if (!isLoading && isAuthenticated) {
    navigate("/");
    return null;
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const success = await register(name, email, password);
    if (success) {
      navigate("/");
    }
  };

  return (
    <PublicLayout>
      <div className="flex min-h-[60vh] items-center justify-center px-4 py-10">
        <div className="w-full max-w-sm">
          <div className="mb-8 text-center">
            <img src={logoImg} alt="Lootera" className="mx-auto mb-4 h-16 w-auto" />
            <h1 className="font-display text-xl font-bold text-foreground">Criar conta</h1>
            <p className="mt-1 text-sm text-muted-foreground">Cadastro conectado ao fluxo real de autenticacao.</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="mb-1 block text-sm text-muted-foreground">Nome</label>
              <input
                value={name}
                onChange={(event) => setName(event.target.value)}
                required
                className="w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                placeholder="Seu nome"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm text-muted-foreground">E-mail</label>
              <input
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                type="email"
                required
                className="w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                placeholder="seu@email.com"
              />
            </div>
            <div>
              <label className="mb-1 block text-sm text-muted-foreground">Senha</label>
              <div className="relative">
                <input
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  type={showPassword ? "text" : "password"}
                  required
                  className="w-full rounded-lg border border-border bg-input py-2.5 pl-4 pr-11 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                  placeholder="Crie uma senha forte"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((current) => !current)}
                  className="absolute right-2 top-1/2 flex h-8 w-8 -translate-y-1/2 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                  aria-label={showPassword ? "Esconder senha" : "Mostrar senha"}
                  title={showPassword ? "Esconder senha" : "Mostrar senha"}
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              <div className="mt-2 rounded-md border border-border/80 bg-card/70 px-3 py-2 text-xs leading-5 text-muted-foreground">
                <p className="font-medium text-foreground">A senha precisa ter:</p>
                <ul className="mt-1 space-y-1">
                  {passwordRequirements.map((requirement) => (
                    <li
                      key={requirement.label}
                      className={requirement.isMet ? "flex items-center gap-2 text-emerald-400" : "flex items-center gap-2"}
                    >
                      <span
                        className={requirement.isMet
                          ? "flex h-4 w-4 items-center justify-center rounded-full bg-emerald-500 text-background"
                          : "h-4 w-4 rounded-full border border-border"
                        }
                        aria-hidden="true"
                      >
                        {requirement.isMet ? <Check className="h-3 w-3" /> : null}
                      </span>
                      <span>{requirement.label}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
            <button type="submit" className="w-full rounded-lg bg-primary py-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90">
              Criar conta
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-muted-foreground">
            Ja tem conta? <Link to="/login" className="text-primary hover:underline">Entrar</Link>
          </p>
        </div>
      </div>
    </PublicLayout>
  );
};

export default Register;
