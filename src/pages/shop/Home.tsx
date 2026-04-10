import { Link } from "react-router-dom";
import PublicLayout from "@/components/lootera/PublicLayout";
import CategoryGrid from "@/components/lootera/CategoryGrid";
import { products } from "@/data/mockData";
import { useCart } from "@/contexts/CartContext";
import { ShoppingCart, Star, ArrowRight } from "lucide-react";
import heroBanner from "@/assets/hero-banner-v3.png";

const Home = () => {
  const { addItem } = useCart();
  const featured = products.filter((p) => p.featured);

  return (
    <PublicLayout>
      {/* Hero */}
      <section className="relative">
        <div className="container mx-auto px-4 pt-4">
          <div className="relative h-[240px] overflow-hidden rounded-xl lg:h-[380px]">
            <img src={heroBanner} alt="Lootera Marketplace" className="absolute inset-0 h-full w-full object-cover" />
            <div className="absolute inset-0 bg-gradient-to-t from-background via-background/40 to-transparent" />
            <div className="absolute bottom-6 left-6 lg:bottom-10 lg:left-10">
              <h1 className="font-display text-2xl font-bold text-foreground lg:text-4xl">Lootera Marketplace</h1>
              <p className="mt-2 max-w-md text-sm text-muted-foreground lg:text-base">O maior marketplace de Tibia. Compre com segurança e entrega imediata.</p>
              <Link to="/produtos" className="mt-4 inline-flex items-center gap-2 rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90">
                Ver produtos <ArrowRight size={16} />
              </Link>
            </div>
          </div>
        </div>
      </section>

      <CategoryGrid variant="commercial" />

      {/* Featured */}
      <section className="pb-10 lg:pb-16">
        <div className="container mx-auto px-4">
          <div className="mb-6 flex items-center justify-between">
            <h2 className="font-display text-lg font-semibold text-foreground lg:text-xl">Destaques</h2>
            <Link to="/produtos" className="text-sm text-primary hover:underline">Ver todos →</Link>
          </div>
          <div className="grid grid-cols-2 gap-4 md:grid-cols-4 lg:gap-6">
            {featured.map((p) => (
              <Link key={p.id} to={`/produto/${p.id}`} className="group flex flex-col rounded-xl border border-border bg-card p-4 transition-all hover:border-primary/30 hover:shadow-lg hover:shadow-primary/5">
                <div className="mb-3 flex h-28 items-center justify-center rounded-lg bg-muted lg:h-36">
                  <span className="text-3xl">🎮</span>
                </div>
                <h3 className="text-sm font-medium text-foreground line-clamp-2 lg:text-base">{p.name}</h3>
                <p className="mt-1 text-xs text-muted-foreground">{p.server}</p>
                <div className="mt-2 flex items-center gap-1">
                  <Star size={12} className="fill-brand-gold text-brand-gold" />
                  <span className="text-xs text-muted-foreground">{p.rating} ({p.sales})</span>
                </div>
                <div className="mt-auto flex items-end justify-between pt-3">
                  <div>
                    {p.originalPrice && <span className="text-xs text-muted-foreground line-through">R$ {p.originalPrice.toFixed(2)}</span>}
                    <p className="text-lg font-bold text-primary">R$ {p.price.toFixed(2)}</p>
                  </div>
                  <button
                    onClick={(e) => { e.preventDefault(); addItem({ productId: p.id, name: p.name, price: p.price, server: p.server, image: p.image }); }}
                    className="rounded-lg bg-primary p-2 text-primary-foreground transition-colors hover:bg-primary/90"
                  >
                    <ShoppingCart size={16} />
                  </button>
                </div>
              </Link>
            ))}
          </div>
        </div>
      </section>

      {/* Trust */}
      <section className="border-t border-border bg-secondary py-10">
        <div className="container mx-auto px-4">
          <div className="grid grid-cols-2 gap-6 text-center lg:grid-cols-4">
            {[
              { num: "10k+", label: "Transações" },
              { num: "99%", label: "Satisfação" },
              { num: "24/7", label: "Suporte" },
              { num: "5 min", label: "Entrega média" },
            ].map((s) => (
              <div key={s.label}>
                <p className="text-2xl font-bold text-primary lg:text-3xl">{s.num}</p>
                <p className="mt-1 text-sm text-muted-foreground">{s.label}</p>
              </div>
            ))}
          </div>
        </div>
      </section>
    </PublicLayout>
  );
};

export default Home;
