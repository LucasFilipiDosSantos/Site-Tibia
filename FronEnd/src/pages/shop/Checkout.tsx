import { useState } from "react";
import { useNavigate } from "react-router-dom";
import PublicLayout from "@/components/lootera/PublicLayout";
import { useCart } from "@/contexts/CartContext";
import { useAuth } from "@/features/auth/context/AuthContext";
import { apiRequest } from "@/lib/api";
import { toast } from "sonner";
import { CreditCard, QrCode, Loader2, CheckCircle2, MessageCircle, TriangleAlert } from "lucide-react";

const WHATSAPP_SUPPORT_NUMBER = "5582999749180";

type PendingCheckoutResponse = {
  orderId: string;
  orderIntentKey: string;
  statusCode: string;
  statusLabel: string;
};

const Checkout = () => {
  const { items, total, clearCart } = useCart();
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [paymentMethod, setPaymentMethod] = useState<"pix" | "card">("pix");
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);

  const [form, setForm] = useState({ name: "", email: "", discord: "" });

  const buildWhatsAppUrl = (pendingOrder?: PendingCheckoutResponse) => {
    const orderLines = items
      .map((item) => {
        const subtotal = item.price * item.quantity;
        return `- ${item.name} x${item.quantity} (${item.server}) - R$ ${subtotal.toFixed(2)}`;
      })
      .join("\n");

    const message = [
      "Ola! Estou tentando finalizar uma compra na Lootera e fui encaminhado para o suporte.",
      "",
      "Itens selecionados:",
      orderLines,
      "",
      `Total: R$ ${total.toFixed(2)}`,
      `Metodo escolhido: ${paymentMethod === "pix" ? "PIX" : "Cartao"}`,
      pendingOrder ? `Pedido pendente: ${pendingOrder.orderId}` : null,
      pendingOrder ? `Referencia: ${pendingOrder.orderIntentKey}` : null,
      "",
      `Nome: ${form.name}`,
      `E-mail: ${form.email}`,
      form.discord ? `Discord: ${form.discord}` : null,
    ].filter(Boolean).join("\n");

    return `https://wa.me/${WHATSAPP_SUPPORT_NUMBER}?text=${encodeURIComponent(message)}`;
  };

  if (items.length === 0 && !success) {
    navigate("/carrinho");
    return null;
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!form.name || !form.email) {
      toast.error("Preencha todos os campos obrigatórios");
      return;
    }
    setLoading(true);

    try {
      const pendingOrder = await apiRequest<PendingCheckoutResponse>("/orders/support-pending", {
        auth: true,
        method: "POST",
        body: JSON.stringify({
          name: form.name,
          email: form.email,
          discord: form.discord || null,
          paymentMethod,
          items: items.map((item) => ({
            productId: item.productId,
            quantity: item.quantity,
            name: item.name,
            price: item.price,
            server: item.server,
          })),
        }),
      });

      toast.info("Compra registrada como pendente. Vamos te encaminhar para o suporte via WhatsApp.");
      clearCart();
      window.location.href = buildWhatsAppUrl(pendingOrder);
    } catch (error) {
      setLoading(false);
      toast.error(error instanceof Error ? error.message : "Nao foi possivel registrar a compra pendente.");
    }
  };

  if (success) {
    return (
      <PublicLayout>
        <div className="container mx-auto px-4 py-20 text-center">
          <CheckCircle2 size={56} className="mx-auto mb-4 text-primary" />
          <h1 className="font-display text-2xl font-bold text-foreground">Pedido confirmado!</h1>
          <p className="mt-2 text-sm text-muted-foreground">Você receberá os detalhes no e-mail informado.</p>
          <button onClick={() => navigate("/produtos")} className="mt-6 rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-primary-foreground hover:bg-primary/90">Continuar comprando</button>
        </div>
      </PublicLayout>
    );
  }

  return (
    <PublicLayout>
      <div className="container mx-auto px-4 py-6 lg:py-10">
        <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">Checkout</h1>

        <form onSubmit={handleSubmit} className="grid gap-8 lg:grid-cols-3">
          <div className="space-y-6 lg:col-span-2">
            {/* Contact info */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h2 className="mb-4 font-display text-base font-semibold text-foreground">Informações de contato</h2>
              <div className="space-y-4">
                <div>
                  <label className="mb-1 block text-sm text-muted-foreground">Nome completo *</label>
                  <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} className="w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary" placeholder="Seu nome" required />
                </div>
                <div>
                  <label className="mb-1 block text-sm text-muted-foreground">E-mail *</label>
                  <input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} type="email" className="w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary" placeholder="seu@email.com" required />
                </div>
                <div>
                  <label className="mb-1 block text-sm text-muted-foreground">Nick do Tibia</label>
                  <input value={form.discord} onChange={(e) => setForm({ ...form, discord: e.target.value })} className="w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary" placeholder="Ex: Knight Level 100" />
                </div>
              </div>
            </div>

            {/* Payment */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h2 className="mb-4 font-display text-base font-semibold text-foreground">Pagamento</h2>
              <div className="mb-4 flex gap-3 rounded-lg border border-primary/30 bg-primary/10 p-4 text-sm text-foreground">
                <TriangleAlert size={20} className="mt-0.5 shrink-0 text-primary" />
                <p>
                  Estamos com problemas no pagamento. Ao confirmar o pedido, vamos te encaminhar para o suporte via WhatsApp.
                </p>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <button type="button" onClick={() => setPaymentMethod("pix")} className={`flex items-center justify-center gap-2 rounded-lg border p-4 text-sm font-medium transition-colors ${paymentMethod === "pix" ? "border-primary bg-primary/10 text-primary" : "border-border text-muted-foreground hover:bg-muted"}`}>
                  <QrCode size={20} /> PIX
                </button>
                <button type="button" onClick={() => setPaymentMethod("card")} className={`flex items-center justify-center gap-2 rounded-lg border p-4 text-sm font-medium transition-colors ${paymentMethod === "card" ? "border-primary bg-primary/10 text-primary" : "border-border text-muted-foreground hover:bg-muted"}`}>
                  <CreditCard size={20} /> Cartão
                </button>
              </div>
              {paymentMethod === "card" && (
                <div className="mt-4 space-y-3">
                  <input className="w-full rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary" placeholder="Número do cartão" />
                  <div className="grid grid-cols-2 gap-3">
                    <input className="rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary" placeholder="MM/AA" />
                    <input className="rounded-lg border border-border bg-input px-4 py-2.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary" placeholder="CVV" />
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Summary */}
          <div className="rounded-xl border border-border bg-card p-6 self-start">
            <h2 className="mb-4 font-display text-base font-semibold text-foreground">Resumo do pedido</h2>
            <div className="space-y-3 mb-4">
              {items.map((item) => (
                <div key={item.productId} className="flex justify-between text-sm">
                  <span className="text-muted-foreground truncate mr-2">{item.name} x{item.quantity}</span>
                  <span className="text-foreground shrink-0">R$ {(item.price * item.quantity).toFixed(2)}</span>
                </div>
              ))}
            </div>
            <div className="border-t border-border pt-3">
              <div className="flex justify-between text-lg font-bold"><span className="text-foreground">Total</span><span className="text-primary">R$ {total.toFixed(2)}</span></div>
            </div>
            <button type="submit" disabled={loading} className="mt-6 flex w-full items-center justify-center gap-2 rounded-lg bg-primary py-3 text-sm font-medium text-primary-foreground transition-colors hover:bg-primary/90 disabled:opacity-50">
              {loading ? <><Loader2 size={16} className="animate-spin" /> Redirecionando...</> : <><MessageCircle size={16} /> Falar com suporte</>}
            </button>
            <p className="mt-3 text-center text-xs text-muted-foreground">Atendimento temporario pelo WhatsApp</p>
          </div>
        </form>
      </div>
    </PublicLayout>
  );
};

export default Checkout;
