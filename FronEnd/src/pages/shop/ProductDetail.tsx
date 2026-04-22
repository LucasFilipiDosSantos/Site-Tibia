import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Minus, Plus, Shield, ShoppingCart, Star } from "lucide-react";
import PublicLayout from "@/components/lootera/PublicLayout";
import { ProductImage } from "@/components/lootera/ProductImage";
import { useCart } from "@/contexts/CartContext";
import { useProduct, useProducts } from "@/features/products/hooks/useProducts";
import type { Product } from "@/features/products/types/product.types";

const ProductRecommendations = ({ products, title }: { products: Product[]; title: string }) => {
  if (products.length === 0) {
    return null;
  }

  return (
    <section className="mt-12">
      <h2 className="mb-4 font-body text-lg font-semibold text-foreground">{title}</h2>
      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        {products.map((relatedProduct) => (
          <Link key={relatedProduct.slug ?? relatedProduct.id} to={`/produto/${relatedProduct.slug ?? relatedProduct.id}`} className="font-body flex flex-col items-start rounded-xl border border-border bg-card p-4 text-left transition-all hover:border-primary/30">
            <ProductImage src={relatedProduct.image} alt={relatedProduct.name} fallbackLabel={relatedProduct.category} className="mb-2 h-20" />
            <h3 className="font-body line-clamp-1 text-sm text-foreground">{relatedProduct.name}</h3>
            <p className="mt-2 text-xs text-muted-foreground">
              <span className="inline-flex rounded border border-border bg-muted/40 px-1.5 py-0.5 text-[10px] leading-none text-foreground">{relatedProduct.server}</span>
              <span className="mx-1.5 text-muted-foreground">|</span>
              {relatedProduct.stock} disponiveis
            </p>
            {relatedProduct.rating > 0 && (
              <div className="mt-2 flex items-center gap-0.5 text-brand-gold" aria-label={`${relatedProduct.rating.toFixed(1)} estrelas`}>
                {Array.from({ length: Math.round(relatedProduct.rating) }).map((_, index) => (
                  <Star key={index} size={12} className="fill-current" />
                ))}
              </div>
            )}
            {relatedProduct.sales > 0 && <p className="mt-1 text-xs text-muted-foreground">{relatedProduct.sales} vendidos</p>}
            <p className="mt-auto pt-3 text-base font-semibold text-primary">R$ {relatedProduct.price.toFixed(2)}</p>
          </Link>
        ))}
      </div>
    </section>
  );
};

const ProductDetail = () => {
  const { slug = "" } = useParams();
  const { addItem } = useCart();
  const [quantity, setQuantity] = useState(1);
  const { data: product, isLoading, isError } = useProduct(slug);
  const { data: relatedProducts = [], isLoading: isLoadingRecommendations } = useProducts({
    category: product?.categorySlug,
    page: 1,
    pageSize: 8,
  });
  const { data: fallbackProducts = [], isLoading: isLoadingFallbackRecommendations } = useProducts({
    page: 1,
    pageSize: 8,
  });

  const related = useMemo(
    () => relatedProducts.filter((candidate) => candidate.slug !== product?.slug).slice(0, 4),
    [product?.slug, relatedProducts],
  );
  const fallbackRecommendations = useMemo(
    () => fallbackProducts.filter((candidate) => candidate.slug !== product?.slug).slice(0, 4),
    [product?.slug, fallbackProducts],
  );
  const recommendations = related.length > 0 ? related : fallbackRecommendations;

  if (isLoading) {
    return (
      <PublicLayout>
        <div className="container mx-auto px-4 py-20 text-center text-sm text-muted-foreground">Carregando produto...</div>
      </PublicLayout>
    );
  }

  if (isError || !product) {
    return (
      <PublicLayout>
        <div className="container mx-auto px-4 py-12 lg:py-16">
          <Link to="/produtos" className="mb-6 inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground">
            <ArrowLeft size={14} /> Voltar
          </Link>

          <div className="max-w-2xl text-left">
            <h1 className="font-body text-2xl font-semibold text-foreground">Produto nao encontrado</h1>
            <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
              Nao encontramos um produto para este link. Veja algumas opcoes disponiveis no catalogo.
            </p>
          </div>

          {isLoadingRecommendations || isLoadingFallbackRecommendations ? (
            <div className="mt-10 text-sm text-muted-foreground">Carregando recomendacoes...</div>
          ) : (
            <ProductRecommendations products={fallbackRecommendations} title="Produtos recomendados" />
          )}
        </div>
      </PublicLayout>
    );
  }

  return (
    <PublicLayout>
      <div className="container mx-auto px-4 py-6 lg:py-10">
        <Link to="/produtos" className="mb-6 inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground">
          <ArrowLeft size={14} /> Voltar
        </Link>

        <div className="grid gap-8 lg:grid-cols-2">
          <ProductImage src={product.image} alt={product.name} fallbackLabel={product.category} className="h-64 rounded-xl border border-border bg-card lg:h-96" />

          <div>
            <span className="mb-2 inline-block rounded-md bg-primary/15 px-2 py-1 text-xs font-medium text-primary">{product.category}</span>
            <h1 className="font-display text-xl font-bold text-foreground lg:text-2xl">{product.name}</h1>
            <p className="mt-1 text-sm text-muted-foreground">Codigo do produto: {product.slug}</p>
            <p className="mt-1 text-sm text-muted-foreground">{product.server} | {product.stock} disponiveis</p>
            {product.rating > 0 && <p className="mt-1 text-sm text-muted-foreground">{product.rating.toFixed(1)} estrelas</p>}
            {product.sales > 0 && <p className="mt-1 text-sm text-muted-foreground">{product.sales} vendidos</p>}

            <div className="mt-6">
              <p className="text-3xl font-bold text-primary">R$ {product.price.toFixed(2)}</p>
            </div>

            <p className="mt-4 text-sm leading-relaxed text-muted-foreground">{product.description}</p>

            <div className="mt-6 flex items-center gap-4">
              <div className="flex items-center rounded-lg border border-border">
                <button onClick={() => setQuantity(Math.max(1, quantity - 1))} className="px-3 py-2 text-muted-foreground transition-colors hover:text-foreground">
                  <Minus size={16} />
                </button>
                <span className="min-w-[2rem] text-center text-sm font-medium text-foreground">{quantity}</span>
                <button onClick={() => setQuantity(quantity + 1)} className="px-3 py-2 text-muted-foreground transition-colors hover:text-foreground">
                  <Plus size={16} />
                </button>
              </div>
              <button
                onClick={() =>
                  addItem(
                    {
                      productId: product.slug ?? product.id,
                      name: product.name,
                      price: product.price,
                      server: product.server,
                      image: product.image,
                    },
                    quantity,
                  )
                }
                className="flex flex-1 items-center justify-center gap-2 rounded-lg bg-brand-gold px-6 py-3 text-sm font-medium text-background transition-colors hover:bg-brand-gold/90"
              >
                <ShoppingCart size={18} /> Adicionar ao carrinho
              </button>
            </div>

            <div className="mt-6 space-y-2">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Shield size={14} className="text-primary" /> Compra protegida com validacao de estoque antes da finalizacao
              </div>
            </div>
          </div>
        </div>

        {isLoadingRecommendations || isLoadingFallbackRecommendations ? (
          <div className="mt-10 text-sm text-muted-foreground">Carregando recomendacoes...</div>
        ) : (
          <ProductRecommendations products={recommendations} title={related.length > 0 ? "Produtos relacionados" : "Produtos recomendados"} />
        )}
      </div>
    </PublicLayout>
  );
};

export default ProductDetail;
