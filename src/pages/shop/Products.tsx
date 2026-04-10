import { useState, useMemo } from "react";
import { Link, useSearchParams } from "react-router-dom";
import PublicLayout from "@/components/lootera/PublicLayout";
import { products, categories, servers } from "@/data/mockData";
import { useCart } from "@/contexts/CartContext";
import { ShoppingCart, Star, SlidersHorizontal, X } from "lucide-react";

const Products = () => {
  const { addItem } = useCart();
  const [searchParams] = useSearchParams();
  const initialCat = searchParams.get("cat") || "";
  const [category, setCategory] = useState(initialCat);
  const [server, setServer] = useState("");
  const [sortBy, setSortBy] = useState("popular");
  const [showFilters, setShowFilters] = useState(false);

  const filtered = useMemo(() => {
    let list = [...products];
    if (category) list = list.filter((p) => p.category === category);
    if (server) list = list.filter((p) => p.server === server || p.server === "Todos");
    switch (sortBy) {
      case "price-asc": list.sort((a, b) => a.price - b.price); break;
      case "price-desc": list.sort((a, b) => b.price - a.price); break;
      case "rating": list.sort((a, b) => b.rating - a.rating); break;
      default: list.sort((a, b) => b.sales - a.sales);
    }
    return list;
  }, [category, server, sortBy]);

  return (
    <PublicLayout>
      <div className="container mx-auto px-4 py-6 lg:py-10">
        <div className="mb-6 flex items-center justify-between">
          <h1 className="font-display text-xl font-bold text-foreground lg:text-2xl">Produtos</h1>
          <button onClick={() => setShowFilters(!showFilters)} className="flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm text-muted-foreground transition-colors hover:bg-secondary lg:hidden">
            <SlidersHorizontal size={16} /> Filtros
          </button>
        </div>

        <div className="flex gap-6">
          {/* Filters sidebar */}
          <aside className={`shrink-0 space-y-6 ${showFilters ? "fixed inset-0 z-50 overflow-auto bg-background p-6 lg:relative lg:inset-auto lg:z-auto lg:bg-transparent lg:p-0" : "hidden lg:block"} lg:w-56`}>
            <div className="flex items-center justify-between lg:hidden">
              <h2 className="font-display text-lg font-semibold text-foreground">Filtros</h2>
              <button onClick={() => setShowFilters(false)} className="text-muted-foreground"><X size={20} /></button>
            </div>

            <div>
              <h3 className="mb-2 text-xs font-semibold uppercase text-muted-foreground">Categoria</h3>
              <div className="space-y-1">
                <button onClick={() => setCategory("")} className={`block w-full rounded-lg px-3 py-2 text-left text-sm transition-colors ${!category ? "bg-primary/15 text-primary" : "text-muted-foreground hover:bg-muted"}`}>Todas</button>
                {categories.map((c) => (
                  <button key={c} onClick={() => setCategory(c)} className={`block w-full rounded-lg px-3 py-2 text-left text-sm transition-colors ${category === c ? "bg-primary/15 text-primary" : "text-muted-foreground hover:bg-muted"}`}>{c}</button>
                ))}
              </div>
            </div>

            <div>
              <h3 className="mb-2 text-xs font-semibold uppercase text-muted-foreground">Servidor</h3>
              <div className="space-y-1">
                <button onClick={() => setServer("")} className={`block w-full rounded-lg px-3 py-2 text-left text-sm transition-colors ${!server ? "bg-primary/15 text-primary" : "text-muted-foreground hover:bg-muted"}`}>Todos</button>
                {servers.map((s) => (
                  <button key={s} onClick={() => setServer(s)} className={`block w-full rounded-lg px-3 py-2 text-left text-sm transition-colors ${server === s ? "bg-primary/15 text-primary" : "text-muted-foreground hover:bg-muted"}`}>{s}</button>
                ))}
              </div>
            </div>

            <div>
              <h3 className="mb-2 text-xs font-semibold uppercase text-muted-foreground">Ordenar</h3>
              <select value={sortBy} onChange={(e) => setSortBy(e.target.value)} className="w-full rounded-lg border border-border bg-input px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary">
                <option value="popular">Mais vendidos</option>
                <option value="price-asc">Menor preço</option>
                <option value="price-desc">Maior preço</option>
                <option value="rating">Melhor avaliação</option>
              </select>
            </div>

            <button onClick={() => setShowFilters(false)} className="w-full rounded-lg bg-primary py-2.5 text-sm font-medium text-primary-foreground lg:hidden">Aplicar Filtros</button>
          </aside>

          {/* Product grid */}
          <div className="flex-1">
            <p className="mb-4 text-sm text-muted-foreground">{filtered.length} produto(s) encontrado(s)</p>
            <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:gap-6">
              {filtered.map((p) => (
                <Link key={p.id} to={`/produto/${p.id}`} className="group flex flex-col rounded-xl border border-border bg-card p-4 transition-all hover:border-primary/30">
                  <div className="mb-3 flex h-24 items-center justify-center rounded-lg bg-muted lg:h-32">
                    <span className="text-2xl">🎮</span>
                  </div>
                  <span className="mb-1 text-[10px] font-medium uppercase text-primary">{p.category}</span>
                  <h3 className="text-sm font-medium text-foreground line-clamp-2">{p.name}</h3>
                  <p className="mt-1 text-xs text-muted-foreground">{p.server}</p>
                  <div className="mt-1 flex items-center gap-1">
                    <Star size={10} className="fill-brand-gold text-brand-gold" />
                    <span className="text-[10px] text-muted-foreground">{p.rating}</span>
                  </div>
                  <div className="mt-auto flex items-end justify-between pt-3">
                    <div>
                      {p.originalPrice && <span className="text-[10px] text-muted-foreground line-through">R$ {p.originalPrice.toFixed(2)}</span>}
                      <p className="text-base font-bold text-primary">R$ {p.price.toFixed(2)}</p>
                    </div>
                    <button onClick={(e) => { e.preventDefault(); addItem({ productId: p.id, name: p.name, price: p.price, server: p.server, image: p.image }); }} className="rounded-lg bg-primary p-2 text-primary-foreground transition-colors hover:bg-primary/90">
                      <ShoppingCart size={14} />
                    </button>
                  </div>
                </Link>
              ))}
            </div>
          </div>
        </div>
      </div>
    </PublicLayout>
  );
};

export default Products;
