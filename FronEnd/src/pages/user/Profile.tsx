import { Link, useNavigate } from "react-router-dom";
import PublicLayout from "@/components/lootera/PublicLayout";
import { useAuth } from "@/features/auth/context/AuthContext";
import { orderService } from "@/features/orders/services/order.service";
import { useQuery } from "@tanstack/react-query";
import { User, Package, LogOut } from "lucide-react";

const Profile = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();
  const { data: userOrders = [] } = useQuery({
    queryKey: ["my-orders"],
    queryFn: () => orderService.getMyOrders(),
    enabled: isAuthenticated,
  });

  if (!isAuthenticated) {
    navigate("/login");
    return null;
  }

  return (
    <PublicLayout>
      <div className="container mx-auto px-4 py-6 lg:py-10">
        <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Meu Perfil</h1>

        <div className="grid gap-6 lg:grid-cols-3">
          <div className="rounded-xl border border-border bg-card p-6 text-center">
            <div className="mx-auto mb-4 flex h-20 w-20 items-center justify-center rounded-full bg-primary/15 text-primary">
              <User size={36} />
            </div>
            <h2 className="font-display text-lg font-semibold text-foreground">{user?.name}</h2>
            <p className="text-sm text-muted-foreground">{user?.email}</p>
            <p className="mt-2 text-xs text-muted-foreground">Membro desde {new Date(user?.createdAt || "").toLocaleDateString("pt-BR")}</p>
          </div>

          <div className="lg:col-span-2 space-y-4">
            <div className="rounded-xl border border-border bg-card p-6">
              <h3 className="mb-4 font-display text-base font-semibold text-foreground">Informações</h3>
              <div className="space-y-3">
                <div><label className="text-xs text-muted-foreground">Nome</label><input defaultValue={user?.name} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
                <div><label className="text-xs text-muted-foreground">E-mail</label><input defaultValue={user?.email} className="mt-1 w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary" /></div>
                <button className="rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">Salvar</button>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <Link to="/pedidos" className="flex items-center gap-3 rounded-xl border border-border bg-card p-6 transition-all hover:border-primary/30">
                <Package size={24} className="text-primary" />
                <div>
                  <p className="text-sm font-medium text-foreground">Meus Pedidos</p>
                  <p className="text-xs text-muted-foreground">{userOrders.length} pedidos</p>
                </div>
              </Link>
              <button onClick={() => { logout(); navigate("/"); }} className="flex items-center gap-3 rounded-xl border border-border bg-card p-6 transition-all hover:border-destructive/30">
                <LogOut size={24} className="text-destructive" />
                <div className="text-left">
                  <p className="text-sm font-medium text-foreground">Sair</p>
                  <p className="text-xs text-muted-foreground">Encerrar sessão</p>
                </div>
              </button>
            </div>
          </div>
        </div>
      </div>
    </PublicLayout>
  );
};

export default Profile;
