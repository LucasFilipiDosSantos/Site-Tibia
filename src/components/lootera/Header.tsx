import { Search, ShoppingCart, User, Menu } from "lucide-react";
import { useState } from "react";
import logoImg from "@/assets/lootera-logo.png";

interface HeaderProps {
  variant?: "commercial" | "fantasy";
}

const Header = ({ variant = "commercial" }: HeaderProps) => {
  const [menuOpen, setMenuOpen] = useState(false);
  const isFantasy = variant === "fantasy";

  return (
    <header className={`sticky top-0 z-50 w-full border-b border-border ${isFantasy ? "bg-brand-purple/80 backdrop-blur-md" : "bg-header backdrop-blur-md"}`}>
      <div className="container mx-auto flex items-center justify-between px-4 py-2 lg:px-6">
        {/* Logo */}
        <div className="flex items-center gap-2 shrink-0">
          <img src={logoImg} alt="Lootera" className="h-10 w-auto lg:h-12" />
        </div>

        {/* Search - Desktop */}
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

        {/* Cart button */}
        <div className="hidden md:flex items-center mr-4">
          <button className="flex items-center justify-center rounded-lg bg-brand-gold px-3 py-2.5 text-background transition-colors hover:bg-brand-gold/90">
            <ShoppingCart size={20} />
          </button>
        </div>

        {/* Actions - Desktop */}
        <div className="hidden items-center gap-3 md:flex">
          <button className="rounded-lg border border-border px-5 py-2 text-sm font-medium text-foreground transition-colors hover:bg-secondary">
            Entrar
          </button>
          <button className="rounded-lg border border-border px-5 py-2 text-sm font-medium text-foreground transition-colors hover:bg-secondary">
            Registro
          </button>
          <button className="text-muted-foreground transition-colors hover:text-foreground">
            <User size={22} />
          </button>
          <button className="text-muted-foreground transition-colors hover:text-foreground">
            <Menu size={22} />
          </button>
        </div>

        {/* Mobile */}
        <div className="flex items-center gap-3 md:hidden">
          <button className="flex items-center justify-center rounded-lg bg-brand-gold p-2 text-background">
            <ShoppingCart size={18} />
          </button>
          <button onClick={() => setMenuOpen(!menuOpen)} className="text-muted-foreground">
            <Menu size={22} />
          </button>
        </div>
      </div>

      {/* Mobile Search */}
      <div className="border-t border-border px-4 py-2 md:hidden">
        <div className="relative flex">
          <button className="flex items-center justify-center rounded-l-lg bg-brand-gold px-3 text-background">
            <Search size={16} />
          </button>
          <input
            type="text"
            placeholder="Pesquise seu Jogo"
            className="w-full rounded-r-lg bg-input py-2 pl-4 pr-4 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary"
          />
        </div>
      </div>

      {menuOpen && (
        <div className="border-t border-border px-4 py-4 md:hidden">
          <div className="flex flex-col gap-3">
            <button className="w-full rounded-lg border border-border py-2.5 text-sm font-medium text-foreground">Entrar</button>
            <button className="w-full rounded-lg border border-border py-2.5 text-sm font-medium text-foreground">Registro</button>
            <button className="flex items-center gap-2 py-2 text-sm text-muted-foreground">
              <User size={18} /> Meu Perfil
            </button>
          </div>
        </div>
      )}
    </header>
  );
};

export default Header;
