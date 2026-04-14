import { Link } from "react-router-dom";
import PublicLayout from "@/components/lootera/PublicLayout";
import { useCart } from "@/contexts/CartContext";
import { Trash2, Plus, Minus, ShoppingBag } from "lucide-react";

const Cart = () => {
  const { items, removeItem, updateQuantity, total, clearCart } = useCart();

  if (items.length === 0) {
    return (
      <PublicLayout>
        <div className="container mx-auto px-4 py-20 text-center">
          <ShoppingBag size={48} className="mx-auto mb-4 text-muted-foreground" />
          <h1 className="font-display text-xl font-bold text-foreground">Carrinho vazio</h1>
          <p className="mt-2 text-sm text-muted-foreground">Adicione produtos para começar.</p>
          <Link to="/produtos" className="mt-6 inline-block rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">Ver produtos</Link>
        </div>
      </PublicLayout>
    );
  }

  return (
    <PublicLayout>
      <div className="container mx-auto px-4 py-6 lg:py-10">
        <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Carrinho</h1>

        <div className="grid gap-8 lg:grid-cols-3">
          <div className="lg:col-span-2 space-y-4">
            {items.map((item) => (
              <div key={item.productId} className="flex items-center gap-4 rounded-xl border border-border bg-card p-4">
                <div className="flex h-16 w-16 shrink-0 items-center justify-center rounded-lg bg-muted">
                  <span className="text-xl">🎮</span>
                </div>
                <div className="flex-1 min-w-0">
                  <h3 className="text-sm font-medium text-foreground truncate">{item.name}</h3>
                  <p className="text-xs text-muted-foreground">{item.server}</p>
                  <p className="mt-1 text-base font-bold text-primary">R$ {item.price.toFixed(2)}</p>
                </div>
                <div className="flex items-center rounded-lg border border-border">
                  <button onClick={() => updateQuantity(item.productId, item.quantity - 1)} className="px-2 py-1 text-muted-foreground hover:text-foreground"><Minus size={14} /></button>
                  <span className="min-w-[1.5rem] text-center text-sm text-foreground">{item.quantity}</span>
                  <button onClick={() => updateQuantity(item.productId, item.quantity + 1)} className="px-2 py-1 text-muted-foreground hover:text-foreground"><Plus size={14} /></button>
                </div>
                <p className="hidden w-20 text-right text-sm font-semibold text-foreground sm:block">R$ {(item.price * item.quantity).toFixed(2)}</p>
                <button onClick={() => removeItem(item.productId)} className="text-muted-foreground hover:text-destructive transition-colors">
                  <Trash2 size={16} />
                </button>
              </div>
            ))}
            <button onClick={clearCart} className="text-sm text-muted-foreground hover:text-destructive transition-colors">Limpar carrinho</button>
          </div>

          {/* Summary */}
          <div className="rounded-xl border border-border bg-card p-6">
            <h2 className="mb-4 font-display text-lg font-semibold text-foreground">Resumo</h2>
            <div className="space-y-2 text-sm">
              <div className="flex justify-between text-muted-foreground"><span>Subtotal</span><span>R$ {total.toFixed(2)}</span></div>
              <div className="flex justify-between text-muted-foreground"><span>Taxa</span><span className="text-primary">Grátis</span></div>
            </div>
            <div className="my-4 border-t border-border" />
            <div className="flex justify-between text-lg font-bold"><span className="text-foreground">Total</span><span className="text-primary">R$ {total.toFixed(2)}</span></div>
            <Link to="/checkout" className="mt-6 block w-full rounded-lg bg-primary py-3 text-center text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90">
              Finalizar compra
            </Link>
          </div>
        </div>
      </div>
    </PublicLayout>
  );
};

export default Cart;
