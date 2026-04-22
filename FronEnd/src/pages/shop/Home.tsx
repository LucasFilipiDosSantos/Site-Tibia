import { Link } from "react-router-dom";
import { ArrowRight, ShoppingCart, Star } from "lucide-react";
import PublicLayout from "@/components/lootera/PublicLayout";
import CategoryGrid from "@/components/lootera/CategoryGrid";
import { ProductImage } from "@/components/lootera/ProductImage";
import { useProducts } from "@/features/products/hooks/useProducts";
import { useCart } from "@/contexts/CartContext";
import heroBanner from "@/assets/hero-banner-v3.png";

const Home = () => {
  const { addItem } = useCart();
  const { data: products = [], isLoading, isError } = useProducts({ page: 1, pageSize: 8 });
  const featured = products.slice(0, 4);

  return (
    <PublicLayout>
      <section className="relative">
        <div className="container mx-auto px-4 pt-4">
          <div className="relative h-[240px] overflow-hidden rounded-xl lg:h-[380px]">
            <img src={heroBanner} alt="Lootera Marketplace" className="absolute inset-0 h-full w-full object-cover" />
            <div className="absolute inset-0 bg-gradient-to-t from-background via-background/40 to-transparent" />
            <div className="absolute bottom-6 left-6 lg:bottom-10 lg:left-10">
              <h1 className="font-display text-2xl font-bold text-foreground lg:text-4xl">Lootera Marketplace</h1>
              <p className="mt-2 max-w-md text-sm text-muted-foreground lg:text-base">
                Encontre coins, itens, personagens e servicos para jogar com mais praticidade.
              </p>
              <Link
                to="/produtos"
                className="mt-4 inline-flex items-center gap-2 rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
              >
                Ver produtos <ArrowRight size={16} />
              </Link>
            </div>
          </div>
        </div>
      </section>

      <CategoryGrid variant="commercial" />

      <section className="pb-10 lg:pb-16">
        <div className="container mx-auto px-4">
          <div className="mb-6 flex items-center justify-between">
            <h2 className="font-display text-lg font-semibold text-foreground lg:text-xl">Catalogo em destaque</h2>
            <Link to="/produtos" className="text-sm text-primary hover:underline">
              Ver todos
            </Link>
          </div>

          {isLoading && <p className="rounded-xl border border-border bg-card p-6 text-sm text-muted-foreground">Carregando produtos...</p>}

          {isError && (
            <p className="rounded-xl border border-destructive/30 bg-destructive/5 p-6 text-sm text-destructive">
              Nao foi possivel carregar o catalogo agora. Tente novamente em instantes.
            </p>
          )}

          {!isLoading && !isError && (
            <div className="grid grid-cols-2 gap-4 md:grid-cols-4 lg:gap-6">
              {featured.map((product) => (
                <Link
                  key={product.slug}
                  to={`/produto/${product.slug}`}
                  className="font-body group flex flex-col items-start rounded-xl border border-border bg-card p-4 text-left transition-all hover:border-primary/30 hover:shadow-lg hover:shadow-primary/5"
                >
                  <ProductImage src={product.image} alt={product.name} fallbackLabel={product.category} className="mb-3 h-28 lg:h-36" />
                  <h3 className="font-body line-clamp-2 text-sm text-foreground lg:text-base">{product.name}</h3>
                  <p className="mt-2 text-xs text-muted-foreground">
                    <span className="inline-flex rounded border border-border bg-muted/40 px-1.5 py-0.5 text-[10px] leading-none text-foreground">{product.server}</span>
                    <span className="mx-1.5 text-muted-foreground">|</span>
                    {product.stock} disponiveis
                  </p>
                  {product.rating > 0 && (
                    <div className="mt-2 flex items-center gap-0.5 text-brand-gold" aria-label={`${product.rating.toFixed(1)} estrelas`}>
                      {Array.from({ length: Math.round(product.rating) }).map((_, index) => (
                        <Star key={index} size={12} className="fill-current" />
                      ))}
                    </div>
                  )}
                  {product.sales > 0 && <p className="mt-1 text-xs text-muted-foreground">{product.sales} vendidos</p>}
                  <div className="mt-auto flex w-full items-end justify-between pt-3">
                    <div>
                      <p className="text-lg font-semibold text-primary">R$ {product.price.toFixed(2)}</p>
                    </div>
                    <button
                      onClick={(event) => {
                        event.preventDefault();
                        addItem({
                          productId: product.slug ?? product.id,
                          name: product.name,
                          price: product.price,
                          server: product.server,
                          image: product.image,
                        });
                      }}
                      className="rounded-lg bg-brand-gold p-2 text-background transition-colors hover:bg-brand-gold/90"
                      aria-label={`Adicionar ${product.name} ao carrinho`}
                    >
                      <ShoppingCart size={16} />
                    </button>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>
    </PublicLayout>
  );
};

export default Home;
