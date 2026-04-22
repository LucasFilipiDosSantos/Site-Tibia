import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import PublicLayout from "@/components/lootera/PublicLayout";
import { useAuth } from "@/features/auth/context/AuthContext";
import logoImg from "@/assets/lootera-logo.png";

const Login = () => {
  const { login, isAuthenticated, isLoading } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  if (!isLoading && isAuthenticated) {
    navigate("/");
    return null;
  }

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    const success = await login(email, password);
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
            <h1 className="font-display text-xl font-bold text-foreground">Entrar</h1>
            <p className="mt-1 text-sm text-muted-foreground">Acesse sua conta para acompanhar compras e pedidos.</p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
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
              <input
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                type="password"
                required
                className="w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
                placeholder="Digite sua senha"
              />
            </div>
            <button type="submit" className="w-full rounded-lg bg-primary py-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90">
              Entrar
            </button>
          </form>

          <p className="mt-4 rounded-lg border border-border bg-card p-3 text-center text-sm text-muted-foreground">
            Se nao conseguir entrar, confira seu e-mail e senha ou tente novamente em instantes.
          </p>

          <p className="mt-6 text-center text-sm text-muted-foreground">
            Nao tem conta? <Link to="/cadastro" className="text-primary hover:underline">Cadastre-se</Link>
          </p>
        </div>
      </div>
    </PublicLayout>
  );
};

export default Login;
