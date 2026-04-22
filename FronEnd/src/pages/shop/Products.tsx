import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ShoppingCart, SlidersHorizontal, X } from "lucide-react";
import PublicLayout from "@/components/lootera/PublicLayout";
import { ProductImage } from "@/components/lootera/ProductImage";
import WorldOptions from "@/components/lootera/WorldOptions";
import { useProducts } from "@/features/products/hooks/useProducts";
import { CATEGORY_OPTIONS } from "@/features/products/utils/catalog";
import { useCart } from "@/contexts/CartContext";

const Products = () => {
  const { addItem } = useCart();
  const [searchParams] = useSearchParams();
  const categoryParam = searchParams.get("category") || "";
  const queryParam = searchParams.get("q")?.trim() || "";
  const [category, setCategory] = useState(categoryParam);
  const [sortBy, setSortBy] = useState("name");
  const [showFilters, setShowFilters] = useState(false);

  useEffect(() => {
    setCategory(categoryParam);
  }, [categoryParam]);

  const { data: products = [], isLoading, isError } = useProducts({
    category: category || undefined,
    page: 1,
    pageSize: queryParam ? 100 : 24,
  });

  const filtered = useMemo(() => {
    const normalizedQuery = queryParam.toLowerCase();
    const list = products.filter((product) => {
      if (!normalizedQuery) {
        return true;
      }

      return [
        product.name,
        product.description,
        product.category,
        product.categorySlug,
        product.server,
        product.slug,
      ]
        .filter(Boolean)
        .some((value) => value?.toLowerCase().includes(normalizedQuery));
    });

    switch (sortBy) {
      case "price-asc":
        list.sort((left, right) => left.price - right.price);
        break;
      case "price-desc":
        list.sort((left, right) => right.price - left.price);
        break;
      default:
        list.sort((left, right) => left.name.localeCompare(right.name));
        break;
    }

    return list;
  }, [products, queryParam, sortBy]);

  return (
    <PublicLayout>
      <div className="container mx-auto px-4 py-6 lg:py-10">
        <div className="mb-6 flex items-center justify-between">
          <div>
            <h1 className="font-display text-xl font-bold text-foreground lg:text-2xl">Produtos</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              {queryParam ? `Resultados para "${queryParam}"` : "Lista alimentada pela API real do catalogo."}
            </p>
          </div>
          <button
            onClick={() => setShowFilters(!showFilters)}
            className="flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm text-muted-foreground transition-colors hover:bg-secondary lg:hidden"
          >
            <SlidersHorizontal size={16} /> Filtros
          </button>
        </div>

        <div className="flex gap-6">
          <aside
            className={`shrink-0 space-y-6 lg:w-56 ${
              showFilters ? "fixed inset-0 z-50 overflow-auto bg-background p-6 lg:relative lg:inset-auto lg:z-auto lg:bg-transparent lg:p-0" : "hidden lg:block"
            }`}
          >
            <div className="flex items-center justify-between lg:hidden">
              <h2 className="font-display text-lg font-semibold text-foreground">Filtros</h2>
              <button onClick={() => setShowFilters(false)} className="text-muted-foreground">
                <X size={20} />
              </button>
            </div>

            <div>
              <h3 className="mb-2 text-xs font-semibold uppercase text-muted-foreground">Categoria</h3>
              <div className="space-y-1">
                <button
                  onClick={() => setCategory("")}
                  className={`block w-full rounded-lg px-3 py-2 text-left text-sm transition-colors ${
                    !category ? "bg-primary/15 text-primary" : "text-muted-foreground hover:bg-muted"
                  }`}
                >
                  Todas
                </button>
                {CATEGORY_OPTIONS.map((option) => (
                  <button
                    key={option.slug}
                    onClick={() => setCategory(option.slug)}
                    className={`block w-full rounded-lg px-3 py-2 text-left text-sm transition-colors ${
                      category === option.slug ? "bg-primary/15 text-primary" : "text-muted-foreground hover:bg-muted"
                    }`}
                  >
                    {option.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <h3 className="mb-2 text-xs font-semibold uppercase text-muted-foreground">Ordenar</h3>
              <select
                value={sortBy}
                onChange={(event) => setSortBy(event.target.value)}
                className="w-full rounded-lg border border-border bg-input px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-primary"
              >
                <option value="name">Nome</option>
                <option value="price-asc">Menor preco</option>
                <option value="price-desc">Maior preco</option>
              </select>
            </div>

            <button
              onClick={() => setShowFilters(false)}
              className="w-full rounded-lg bg-primary py-2.5 text-sm font-medium text-primary-foreground lg:hidden"
            >
              Aplicar filtros
            </button>
          </aside>

          <div className="flex-1">
            {isLoading && <p className="rounded-xl border border-border bg-card p-6 text-sm text-muted-foreground">Carregando produtos...</p>}

            {isError && (
              <p className="rounded-xl border border-destructive/30 bg-destructive/5 p-6 text-sm text-destructive">
                Nao foi possivel carregar os produtos. Confirme se o backend esta respondendo em `VITE_API_BASE_URL`.
              </p>
            )}

            {!isLoading && !isError && (
              <>
                <div className="mb-4 flex flex-wrap items-center gap-3 text-sm text-muted-foreground">
                  <span>{filtered.length} produto(s) encontrado(s)</span>
                  {queryParam && (
                    <Link to={category ? `/produtos?category=${encodeURIComponent(category)}` : "/produtos"} className="text-primary hover:underline">
                      Limpar busca
                    </Link>
                  )}
                </div>
                <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:gap-6">
                  {filtered.map((product) => (
                    <Link
                      key={product.slug}
                      to={`/produto/${product.slug}`}
                      className="group flex flex-col rounded-xl border border-border bg-card p-4 transition-all hover:border-primary/30"
                    >
                      <ProductImage src={product.image} alt={product.name} fallbackLabel={product.category} className="mb-3 h-24 lg:h-32" />
                      <span className="mb-1 text-[10px] font-medium uppercase text-primary">{product.category}</span>
                      <h3 className="line-clamp-2 text-sm font-medium text-foreground">{product.name}</h3>
                      <p className="mt-1 text-xs text-muted-foreground">Servidor: {product.server}</p>
                      <p className="mt-1 text-xs text-muted-foreground">Estoque: {product.stock} disponiveis</p>
                      <p className="mt-1 text-xs text-muted-foreground">{product.rating.toFixed(1)} estrelas · {product.sales} vendas</p>
                      <WorldOptions />
                      <div className="mt-auto flex items-end justify-between pt-3">
                        <p className="text-base font-bold text-primary">R$ {product.price.toFixed(2)}</p>
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
                          className="rounded-lg bg-primary p-2 text-primary-foreground transition-colors hover:bg-primary/90"
                        >
                          <ShoppingCart size={14} />
                        </button>
                      </div>
                    </Link>
                  ))}
                </div>
                {filtered.length === 0 && (
                  <p className="rounded-xl border border-border bg-card p-6 text-sm text-muted-foreground">
                    Nenhum produto encontrado para essa busca.
                  </p>
                )}
              </>
            )}
          </div>
        </div>
      </div>
    </PublicLayout>
  );
};

export default Products;
