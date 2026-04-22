import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Minus, Plus, Shield, ShoppingCart } from "lucide-react";
import PublicLayout from "@/components/lootera/PublicLayout";
import { ProductImage } from "@/components/lootera/ProductImage";
import WorldOptions from "@/components/lootera/WorldOptions";
import { useCart } from "@/contexts/CartContext";
import { useProduct, useProducts } from "@/features/products/hooks/useProducts";

const ProductDetail = () => {
  const { slug = "" } = useParams();
  const { addItem } = useCart();
  const [quantity, setQuantity] = useState(1);
  const { data: product, isLoading, isError } = useProduct(slug);
  const { data: relatedProducts = [] } = useProducts({
    category: product?.categorySlug,
    page: 1,
    pageSize: 4,
  });

  const related = useMemo(
    () => relatedProducts.filter((candidate) => candidate.slug !== product?.slug).slice(0, 4),
    [product?.slug, relatedProducts],
  );

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
        <div className="container mx-auto px-4 py-20 text-center">
          <h1 className="font-display text-2xl font-bold text-foreground">Produto nao encontrado</h1>
          <Link to="/produtos" className="mt-4 inline-block text-primary hover:underline">
            Voltar aos produtos
          </Link>
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
            <p className="mt-1 text-sm text-muted-foreground">Slug publico: {product.slug}</p>
            <p className="mt-1 text-sm text-muted-foreground">Servidor: {product.server}</p>
            <p className="mt-1 text-sm text-muted-foreground">Estoque: {product.stock} disponiveis</p>
            <p className="mt-1 text-sm text-muted-foreground">{product.rating.toFixed(1)} estrelas · {product.sales} vendas</p>
            <WorldOptions />

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
                className="flex flex-1 items-center justify-center gap-2 rounded-lg bg-primary px-6 py-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
              >
                <ShoppingCart size={18} /> Adicionar ao carrinho
              </button>
            </div>

            <div className="mt-6 space-y-2">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Shield size={14} className="text-primary" /> Pedido validado no backend antes do checkout final
              </div>
            </div>

            <p className="mt-4 text-xs text-muted-foreground">
              Estoque e servidor ainda nao sao entregues pelo endpoint publico de catalogo.
            </p>
          </div>
        </div>

        {related.length > 0 && (
          <section className="mt-12">
            <h2 className="mb-4 font-display text-lg font-semibold text-foreground">Produtos relacionados</h2>
            <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
              {related.map((relatedProduct) => (
                <Link key={relatedProduct.slug} to={`/produto/${relatedProduct.slug}`} className="rounded-xl border border-border bg-card p-4 transition-all hover:border-primary/30">
                  <ProductImage src={relatedProduct.image} alt={relatedProduct.name} fallbackLabel={relatedProduct.category} className="mb-2 h-20" />
                  <h3 className="line-clamp-1 text-sm font-medium text-foreground">{relatedProduct.name}</h3>
                  <WorldOptions />
                  <p className="mt-1 text-base font-bold text-primary">R$ {relatedProduct.price.toFixed(2)}</p>
                </Link>
              ))}
            </div>
          </section>
        )}
      </div>
    </PublicLayout>
  );
};

export default ProductDetail;
