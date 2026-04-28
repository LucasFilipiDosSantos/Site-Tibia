import { type ReactNode } from "react";
import { Link, useLocation } from "react-router-dom";
import { LayoutDashboard, Package, ShoppingBag, Users, BarChart3, Settings, ArrowLeft, Menu, X } from "lucide-react";
import { useState } from "react";
import logoImg from "@/assets/lootera-logo.png";

const navItems = [
  { label: "Dashboard", path: "/admin", icon: LayoutDashboard },
  { label: "Produtos", path: "/admin/produtos", icon: Package },
  { label: "Pedidos", path: "/admin/pedidos", icon: ShoppingBag },
  { label: "Usuários", path: "/admin/usuarios", icon: Users },
  { label: "Estoque", path: "/admin/estoque", icon: BarChart3 },
  { label: "Configurações", path: "/admin/configuracoes", icon: Settings },
];

const AdminLayout = ({ children }: { children: ReactNode }) => {
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="min-h-screen flex bg-background">
      <aside className="hidden w-60 shrink-0 flex-col border-r border-border bg-secondary lg:flex">
        <div className="flex items-center gap-2 border-b border-border px-4 py-3">
          <Link to="/" aria-label="Ir para a home da loja" className="rounded transition-opacity hover:opacity-85">
            <img src={logoImg} alt="Lootera" className="h-8 w-auto" />
          </Link>
          <span className="font-display text-xs font-semibold text-brand-gold">Admin</span>
        </div>
        <nav className="flex-1 px-3 py-4 space-y-1">
          {navItems.map((item) => {
            const active = location.pathname === item.path;
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${active ? "bg-primary/15 text-primary" : "text-muted-foreground hover:bg-muted hover:text-foreground"}`}
              >
                <item.icon size={18} />
                {item.label}
              </Link>
            );
          })}
        </nav>
        <div className="border-t border-border px-3 py-3">
          <Link to="/" className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:text-foreground">
            <ArrowLeft size={16} /> Voltar à loja
          </Link>
        </div>
      </aside>

      <div className="flex flex-1 flex-col">
        <header className="flex items-center justify-between border-b border-border bg-secondary px-4 py-3 lg:hidden">
          <div className="flex items-center gap-2">
            <Link to="/" aria-label="Ir para a home da loja" className="rounded transition-opacity hover:opacity-85">
              <img src={logoImg} alt="Lootera" className="h-8 w-auto" />
            </Link>
            <span className="font-display text-xs font-semibold text-brand-gold">Admin</span>
          </div>
          <button onClick={() => setSidebarOpen(!sidebarOpen)} className="text-muted-foreground">
            {sidebarOpen ? <X size={22} /> : <Menu size={22} />}
          </button>
        </header>

        {sidebarOpen && (
          <div className="fixed inset-0 z-50 lg:hidden">
            <div className="absolute inset-0 bg-background/80" onClick={() => setSidebarOpen(false)} />
            <aside className="relative z-10 w-60 h-full border-r border-border bg-secondary flex flex-col">
              <div className="flex items-center justify-between border-b border-border px-4 py-3">
                <span className="font-display text-sm font-semibold text-brand-gold">Menu Admin</span>
                <button onClick={() => setSidebarOpen(false)} className="text-muted-foreground"><X size={20} /></button>
              </div>
              <nav className="flex-1 px-3 py-4 space-y-1">
                {navItems.map((item) => {
                  const active = location.pathname === item.path;
                  return (
                    <Link
                      key={item.path}
                      to={item.path}
                      onClick={() => setSidebarOpen(false)}
                      className={`flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors ${active ? "bg-primary/15 text-primary" : "text-muted-foreground hover:bg-muted hover:text-foreground"}`}
                    >
                      <item.icon size={18} />
                      {item.label}
                    </Link>
                  );
                })}
              </nav>
              <div className="border-t border-border px-3 py-3">
                <Link to="/" onClick={() => setSidebarOpen(false)} className="flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-muted-foreground transition-colors hover:text-foreground">
                  <ArrowLeft size={16} /> Voltar à loja
                </Link>
              </div>
            </aside>
          </div>
        )}

        <main className="flex-1 overflow-auto p-4 lg:p-6">{children}</main>
      </div>
    </div>
  );
};

export default AdminLayout;
