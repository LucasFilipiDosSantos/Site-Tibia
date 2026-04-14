import { useParams, Link } from "react-router-dom";
import PublicLayout from "@/components/lootera/PublicLayout";
import { productService } from "@/features/products/services/product.service";
import { useCart } from "@/contexts/CartContext";
import { ShoppingCart, Star, Shield, Clock, ArrowLeft, Plus, Minus } from "lucide-react";
import { useState } from "react";

const ProductDetail = () => {
  const { id } = useParams();
  const product = productService.getProductById(id!);
  const { addItem } = useCart();
  const [qty, setQty] = useState(1);

  if (!product) {
    return (
      <PublicLayout>
        <div className="container mx-auto px-4 py-20 text-center">
          <h1 className="font-display text-2xl font-bold text-foreground">Produto não encontrado</h1>
          <Link to="/produtos" className="mt-4 inline-block text-primary hover:underline">← Voltar aos produtos</Link>
        </div>
      </PublicLayout>
    );
  }

  const related = productService.getProductsByCategory(product.category).filter(p => p.id !== product.id).slice(0, 4);

  return (
    <PublicLayout>
      <div className="container mx-auto px-4 py-6 lg:py-10">
        <Link to="/produtos" className="mb-6 inline-flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors">
          <ArrowLeft size={14} /> Voltar
        </Link>

        <div className="grid gap-8 lg:grid-cols-2">
          {/* Image */}
          <div className="flex h-64 items-center justify-center rounded-xl border border-border bg-card lg:h-96">
            <span className="text-6xl">🎮</span>
          </div>

          {/* Details */}
          <div>
            <span className="mb-2 inline-block rounded-md bg-primary/15 px-2 py-1 text-xs font-medium text-primary">{product.category}</span>
            <h1 className="font-display text-xl font-bold text-foreground lg:text-2xl">{product.name}</h1>
            <p className="mt-1 text-sm text-muted-foreground">Servidor: {product.server}</p>

            <div className="mt-3 flex items-center gap-2">
              <div className="flex items-center gap-1">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Star key={i} size={14} className={i < Math.round(product.rating) ? "fill-brand-gold text-brand-gold" : "text-muted"} />
                ))}
              </div>
              <span className="text-sm text-muted-foreground">{product.rating} · {product.sales} vendas</span>
            </div>

            <div className="mt-6">
              {product.originalPrice && <span className="text-sm text-muted-foreground line-through">R$ {product.originalPrice.toFixed(2)}</span>}
              <p className="text-3xl font-bold text-primary">R$ {product.price.toFixed(2)}</p>
            </div>

            <p className="mt-4 text-sm leading-relaxed text-muted-foreground">{product.description}</p>

            <div className="mt-6 flex items-center gap-4">
              <div className="flex items-center rounded-lg border border-border">
                <button onClick={() => setQty(Math.max(1, qty - 1))} className="px-3 py-2 text-muted-foreground hover:text-foreground transition-colors"><Minus size={16} /></button>
                <span className="min-w-[2rem] text-center text-sm font-medium text-foreground">{qty}</span>
                <button onClick={() => setQty(qty + 1)} className="px-3 py-2 text-muted-foreground hover:text-foreground transition-colors"><Plus size={16} /></button>
              </div>
              <button
                onClick={() => addItem({ productId: product.id, name: product.name, price: product.price, server: product.server, image: product.image }, qty)}
                className="flex flex-1 items-center justify-center gap-2 rounded-lg bg-primary px-6 py-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90"
              >
                <ShoppingCart size={18} /> Adicionar ao carrinho
              </button>
            </div>

            <div className="mt-6 space-y-2">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Clock size={14} className="text-primary" /> Entrega em até 5 minutos
              </div>
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Shield size={14} className="text-primary" /> Transação 100% segura
              </div>
            </div>

            <p className="mt-4 text-xs text-muted-foreground">Estoque: {product.stock > 10 ? "Disponível" : `${product.stock} restantes`}</p>
          </div>
        </div>

        {/* Related */}
        {related.length > 0 && (
          <section className="mt-12">
            <h2 className="mb-4 font-display text-lg font-semibold text-foreground">Produtos relacionados</h2>
            <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
              {related.map((p) => (
                <Link key={p.id} to={`/produto/${p.id}`} className="rounded-xl border border-border bg-card p-4 transition-all hover:border-primary/30">
                  <div className="mb-2 flex h-20 items-center justify-center rounded-lg bg-muted"><span className="text-xl">🎮</span></div>
                  <h3 className="text-sm font-medium text-foreground line-clamp-1">{p.name}</h3>
                  <p className="mt-1 text-base font-bold text-primary">R$ {p.price.toFixed(2)}</p>
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
