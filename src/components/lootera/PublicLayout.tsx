import { type ReactNode } from "react";
import { Link } from "react-router-dom";
import { Search, ShoppingCart, User, Menu, X } from "lucide-react";
import { useState } from "react";
import { useCart } from "@/contexts/CartContext";
import { useAuth } from "@/contexts/AuthContext";
import logoImg from "@/assets/lootera-logo.png";
import Footer from "./Footer";
import FloatingSupport from "./FloatingSupport";

const ShopHeader = () => {
  const [menuOpen, setMenuOpen] = useState(false);
  const { itemCount } = useCart();
  const { isAuthenticated, isAdmin, user, logout } = useAuth();

  return (
    <header className="sticky top-0 z-50 w-full border-b border-border bg-header backdrop-blur-md">
      <div className="container mx-auto flex items-center justify-between px-4 py-2 lg:px-6">
        <Link to="/" className="flex items-center gap-2 shrink-0">
          <img src={logoImg} alt="Lootera" className="h-10 w-auto lg:h-12" />
        </Link>

        <div className="hidden flex-1 max-w-xl mx-6 md:flex">
          <div className="relative w-full flex">
            <button className="flex items-center justify-center rounded-l-lg bg-brand-gold px-3 text-background">
              <Search size={18} />
            </button>
            <input
              type="text"
              placeholder="Pesquise seu Jogo"
              className="w-full rounded-r-lg bg-input py-2.5 pl-4 pr-4 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
            />
          </div>
        </div>

        <div className="hidden md:flex items-center gap-3">
          <Link to="/carrinho" className="relative flex items-center justify-center rounded-lg bg-brand-gold px-3 py-2.5 text-background transition-colors hover:bg-brand-gold/90">
            <ShoppingCart size={20} />
            {itemCount > 0 && (
              <span className="absolute -right-1.5 -top-1.5 flex h-5 w-5 items-center justify-center rounded-full bg-destructive text-[10px] font-bold text-destructive-foreground">{itemCount}</span>
            )}
          </Link>
          {isAuthenticated ? (
            <>
              {isAdmin && (
                <Link to="/admin" className="rounded-lg border border-primary px-4 py-2 text-sm font-medium text-primary transition-colors hover:bg-primary/10">Admin</Link>
              )}
              <Link to="/perfil" className="rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground transition-colors hover:bg-secondary flex items-center gap-2">
                <User size={16} /> {user?.name?.split(" ")[0]}
              </Link>
              <button onClick={logout} className="rounded-lg border border-border px-4 py-2 text-sm font-medium text-muted-foreground transition-colors hover:bg-secondary">Sair</button>
            </>
          ) : (
            <>
              <Link to="/login" className="rounded-lg border border-border px-5 py-2 text-sm font-medium text-foreground transition-colors hover:bg-secondary">Entrar</Link>
              <Link to="/cadastro" className="rounded-lg bg-primary px-5 py-2 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90">Registro</Link>
            </>
          )}
        </div>

        <div className="flex items-center gap-3 md:hidden">
          <Link to="/carrinho" className="relative flex items-center justify-center rounded-lg bg-brand-gold p-2 text-background">
            <ShoppingCart size={18} />
            {itemCount > 0 && (
              <span className="absolute -right-1 -top-1 flex h-4 w-4 items-center justify-center rounded-full bg-destructive text-[9px] font-bold text-destructive-foreground">{itemCount}</span>
            )}
          </Link>
          <button onClick={() => setMenuOpen(!menuOpen)} className="text-muted-foreground">
            {menuOpen ? <X size={22} /> : <Menu size={22} />}
          </button>
        </div>
      </div>

      <div className="border-t border-border px-4 py-2 md:hidden">
        <div className="relative flex">
          <button className="flex items-center justify-center rounded-l-lg bg-brand-gold px-3 text-background">
            <Search size={16} />
          </button>
          <input type="text" placeholder="Pesquise seu Jogo" className="w-full rounded-r-lg bg-input py-2 pl-4 pr-4 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary" />
        </div>
      </div>

      {menuOpen && (
        <div className="border-t border-border px-4 py-4 md:hidden">
          <div className="flex flex-col gap-3">
            <Link to="/produtos" onClick={() => setMenuOpen(false)} className="w-full rounded-lg border border-border py-2.5 text-center text-sm font-medium text-foreground">Produtos</Link>
            {isAuthenticated ? (
              <>
                {isAdmin && <Link to="/admin" onClick={() => setMenuOpen(false)} className="w-full rounded-lg border border-primary py-2.5 text-center text-sm font-medium text-primary">Admin</Link>}
                <Link to="/perfil" onClick={() => setMenuOpen(false)} className="w-full rounded-lg border border-border py-2.5 text-center text-sm font-medium text-foreground">Meu Perfil</Link>
                <Link to="/pedidos" onClick={() => setMenuOpen(false)} className="w-full rounded-lg border border-border py-2.5 text-center text-sm font-medium text-foreground">Meus Pedidos</Link>
                <button onClick={() => { logout(); setMenuOpen(false); }} className="w-full rounded-lg border border-border py-2.5 text-sm font-medium text-muted-foreground">Sair</button>
              </>
            ) : (
              <>
                <Link to="/login" onClick={() => setMenuOpen(false)} className="w-full rounded-lg border border-border py-2.5 text-center text-sm font-medium text-foreground">Entrar</Link>
                <Link to="/cadastro" onClick={() => setMenuOpen(false)} className="w-full rounded-lg bg-primary py-2.5 text-center text-sm font-medium text-primary-foreground">Registro</Link>
              </>
            )}
          </div>
        </div>
      )}
    </header>
  );
};

const PublicLayout = ({ children }: { children: ReactNode }) => {
  return (
    <div className="min-h-screen flex flex-col bg-background">
      <ShopHeader />
      <main className="flex-1">{children}</main>
      <Footer />
      <FloatingSupport />
    </div>
  );
};

export default PublicLayout;
