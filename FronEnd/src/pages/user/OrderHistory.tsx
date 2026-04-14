import PublicLayout from "@/components/lootera/PublicLayout";
import { useAuth } from "@/features/auth/context/AuthContext";
import { orderService } from "@/features/orders/services/order.service";

const statusColors: Record<string, string> = {
  pending: "bg-brand-gold/15 text-brand-gold",
  processing: "bg-primary/15 text-primary",
  completed: "bg-green-500/15 text-green-400",
  cancelled: "bg-destructive/15 text-destructive",
  refunded: "bg-muted text-muted-foreground",
};

const statusLabels: Record<string, string> = {
  pending: "Pendente",
  processing: "Processando",
  completed: "Concluído",
  cancelled: "Cancelado",
  refunded: "Reembolsado",
};

const OrderHistory = () => {
  const { user } = useAuth();

  if (!user) return null;

  const userOrders = orderService.getOrdersByUserId(user.id);

  return (
    <PublicLayout>
      <div className="container mx-auto px-4 py-6 lg:py-10">
        <h1 className="mb-6 font-display text-xl font-bold text-foreground lg:text-2xl">
          Meus Pedidos
        </h1>

        <div className="space-y-4">
          {userOrders.map((order) => (
            <div
              key={order.id}
              className="rounded-xl border border-border bg-card p-4 lg:p-6"
            >
              <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <div className="flex items-center gap-3">
                    <h3 className="text-sm font-semibold text-foreground">
                      {order.id}
                    </h3>
                    <span
                      className={`rounded-md px-2 py-0.5 text-xs font-medium ${
                        statusColors[order.status]
                      }`}
                    >
                      {statusLabels[order.status]}
                    </span>
                  </div>

                  <p className="mt-1 text-xs text-muted-foreground">
                    {new Date(order.createdAt).toLocaleDateString("pt-BR")} ·{" "}
                    {order.paymentMethod}
                  </p>
                </div>

                <p className="text-lg font-bold text-primary">
                  R$ {order.total.toFixed(2)}
                </p>
              </div>

              <div className="mt-3 border-t border-border pt-3">
                {order.items.map((item) => (
                  <div
                    key={item.productId}
                    className="flex justify-between text-sm"
                  >
                    <span className="text-muted-foreground">
                      {item.name} x{item.quantity}
                    </span>
                    <span className="text-foreground">
                      R$ {(item.price * item.quantity).toFixed(2)}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </PublicLayout>
  );
};

export default OrderHistory;